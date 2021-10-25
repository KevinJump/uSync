using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Mapping;

namespace uSync.Community.Contrib.Mappers
{
    /// <summary>
    ///  value mapper for Contentment content blocks. 
    /// </summary>
    public class ContentmentContentBlocks : SyncNestedValueMapperBase, ISyncMapper
    {
        public ContentmentContentBlocks(IEntityService entityService, Lazy<SyncValueMapperCollection> mapperCollection, IContentTypeService contentTypeService, IDataTypeService dataTypeService)
            : base(entityService, mapperCollection, contentTypeService, dataTypeService)
        { }

        public override string Name => "Contentment content block mapper";

        public override string[] Editors => new string[]
        {
            "Umbraco.Community.Contentment.ContentBlocks"
        };

        /// <summary>
        ///  get the formatted export value, 
        /// </summary>
        /// <remarks>
        ///  for 99% of the time you don't need to do this, but there can be formatting
        ///  issues with internal values (notably Umbraco.Datattime) so we recurse in to 
        ///  ensure the format of other nested values comes out at we expect it to.
        /// </remarks>
        /// <param name="value"></param>
        /// <param name="editorAlias"></param>
        /// <returns></returns>
        public override string GetExportValue(object value, string editorAlias)
        {
            var stringValue = GetValueAs<string>(value);
            if (string.IsNullOrWhiteSpace(stringValue) || !stringValue.DetectIsJson()) return value.ToString();

            var elements = JsonConvert.DeserializeObject<JArray>(stringValue);
            if (elements == null || !elements.Any()) return value.ToString();

            foreach(var item in elements.Cast<JObject>())
            {
                var itemValue = item.Value<JObject>("value");
                if (itemValue == null) continue;

                var doctype = GetDocTypeByKey(item, "elementType");
                if (doctype == null) continue;

                GetExportProperties(itemValue, doctype);
            }

            return JsonConvert.SerializeObject(elements);
        }

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var stringValue = GetValueAs<string>(value);
            if (string.IsNullOrWhiteSpace(stringValue) || !stringValue.DetectIsJson()) return Enumerable.Empty<uSyncDependency>();

            var elements = JsonConvert.DeserializeObject<JArray>(stringValue);
            if (elements == null || !elements.Any()) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach(var item in elements.Cast<JObject>())
            {
                var itemValue = item.Value<JObject>("value");
                if (itemValue == null) continue;

                var doctype = GetDocTypeByKey(item, "elementType");
                if (doctype == null) continue;

                if (flags.HasFlag(DependencyFlags.IncludeDependencies))
                {
                    var doctypeDependency = CreateDocTypeDependency(doctype.Alias, flags);
                    if (doctypeDependency != null) dependencies.Add(doctypeDependency);
                }

                dependencies.AddRange(GetPropertyDependencies(itemValue, doctype, flags));
            }

            return dependencies;
        }
    }
}
