using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

using uSync.Core.Cache;

namespace uSync.Core.Mapping
{
    public class SyncValueMapperCollection
            : BuilderCollectionBase<ISyncMapper>
    {
        private readonly IDictionary<string, string> CustomMappings;

        public SyncEntityCache EntityCache { get; private set; }

        public SyncValueMapperCollection(
            SyncEntityCache entityCache,
            Func<IEnumerable<ISyncMapper>> items)
            : base(items)
        {
            EntityCache = entityCache;

            // todo, load these from config. 
            CustomMappings = new Dictionary<string, string>();
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

                return GetCleanFlatJson(mappedValue);
            }

            return value;
        }

        /// <summary>
        ///  cleans and flattens the JSON , so the stuff we import doesn't actually have all the spaces in it. 
        /// </summary>
        private string GetCleanFlatJson(string stringValue)
        {
            if (stringValue.TryParseValidJsonString(out JToken result) is false) 
                return stringValue;
            try
            {
                return JsonConvert.SerializeObject(result);
            }
            catch
            {
                return stringValue;
            }
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
    }

    public class SyncValueMapperCollectionBuilder
        // : WeightedCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
        : LazyCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
    {
        protected override SyncValueMapperCollectionBuilder This => this;
    }
}
