using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;

namespace uSync8.ContentEdition.Mapping
{
    public class SyncValueMapperCollection
            : BuilderCollectionBase<ISyncMapper>
    {

        public SyncValueMapperCollection(IEnumerable<ISyncMapper> items)
            : base(items) { }

        [Obsolete("Request all mappers and you can chain multiple mappers")]
        public ISyncMapper GetSyncMapper(string alias)
        {
            return this.FirstOrDefault(x => x.Editors.InvariantContains(alias));
        }

        public IEnumerable<ISyncMapper> GetSyncMappers(string alias)
            => this.Where(x => x.Editors.InvariantContains(alias));

        [Obsolete("Request all mappers and you can chain multiple mappers")]
        public ISyncMapper GetSyncMapper(PropertyType propertyType)
        {
            return this.FirstOrDefault(x => x.IsMapper(propertyType));
        }

        public string GetExportValue(object value, string editorAlias)
        {
            if (value == null) return string.Empty;

            var mappers = GetSyncMappers(editorAlias);
            if (mappers.Any())
            {
                var mappedValue = value.ToString();
                foreach (var mapper in mappers)
                {
                    mappedValue = mapper.GetExportValue(mappedValue, editorAlias);
                }

                return mappedValue;
            }

            return GetSafeValue(value);
        }

        public object GetImportValue(string value, string editorAlias)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var mappers = GetSyncMappers(editorAlias);
            if (mappers.Any())
            {
                var mappedValue = value;
                foreach(var mapper in mappers)
                {
                    mappedValue = mapper.GetImportValue(mappedValue, editorAlias);
                }
                return mappedValue;
            }

            return value;
        }

        /// <summary>
        ///  Ensure we get a globally portable string for a value
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   it should be the responsiblity of the mapper to do this
        ///   but there are times (such as dates and times) when its 
        ///   better to ensure all values of a certain type leave 
        ///   using the same format. 
        ///  </para>
        /// </remarks>
        private string GetSafeValue(object value)
        {
            switch (value)
            {
                case DateTime date:
                    // use the Sortable 's' format of ISO 8601, it doesn't include milliseconds
                    // so we get less false positives, on checking and everything is to the second.
                    return date.ToString("s"); 
                default:
                    return value.ToString();
            }
        }
    }

    public class SyncValueMapperCollectionBuilder
        : WeightedCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
    {
        protected override SyncValueMapperCollectionBuilder This => this;
    }
}
