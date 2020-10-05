using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;

using uSync8.BackOffice.Configuration;

namespace uSync8.ContentEdition.Mapping
{
    public class SyncValueMapperCollection
            : BuilderCollectionBase<ISyncMapper>
    {
        private readonly IDictionary<string, string> CustomMappings;

        public SyncValueMapperCollection(
            uSyncConfig uSyncConfig,
            IEnumerable<ISyncMapper> items)
            : base(items)
        {
            CustomMappings = uSyncConfig.Settings.CustomMappings;
        }

        /// <summary>
        ///  Returns the syncMappers assocated with the properyEditorAlias
        /// </summary>
        public IEnumerable<ISyncMapper> GetSyncMappers(string editorAlias)
        {
            var mappedAlias = GetMapperAlias(editorAlias);
            return this.Where(x => x.Editors.InvariantContains(mappedAlias));
        }

        /// <summary>
        ///  Get the mapped export value
        /// </summary>
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

        /// <summary>
        ///  Get the mapped import value
        /// </summary>
        public object GetImportValue(string value, string editorAlias)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var mappers = GetSyncMappers(editorAlias);
            if (mappers.Any())
            {
                var mappedValue = value;
                foreach (var mapper in mappers)
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

        /// <summary>
        ///  looks up the alias for a mapper (replacing it from settings if need be)
        /// </summary>
        private string GetMapperAlias(string alias)
        {
            if (CustomMappings.ContainsKey(alias.ToLower()))
                return CustomMappings[alias.ToLower()];

            return alias;
        }


        // Obsolete calls. 

        [Obsolete("Request all mappers and you can chain multiple mappers")]
        public ISyncMapper GetSyncMapper(PropertyType propertyType)
            => this.FirstOrDefault(x => x.IsMapper(propertyType));

        [Obsolete("Request all mappers and you can chain multiple mappers")]
        public ISyncMapper GetSyncMapper(string alias)
            => this.FirstOrDefault(x => x.Editors.InvariantContains(GetMapperAlias(alias)));

    }

    public class SyncValueMapperCollectionBuilder
        : WeightedCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
    {
        protected override SyncValueMapperCollectionBuilder This => this;
    }
}
