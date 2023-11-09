using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Mapping;

namespace uSync.Community.Contrib.Mappers
{
    /// <summary>
    ///  value mapper for Contentment content blocks. 
    /// </summary>
    public class ContentmentContentBlocks : SyncNestedJsonValueMapperBase, ISyncMapper
    {
        public ContentmentContentBlocks(IEntityService entityService, Lazy<SyncValueMapperCollection> mapperCollection, IContentTypeService contentTypeService, IDataTypeService dataTypeService)
            : base(entityService, mapperCollection, contentTypeService, dataTypeService)
        { }

        public override string Name => "Contentment content block mapper";

        public override string[] Editors => new string[]
        {
            "Umbraco.Community.Contentment.ContentBlocks"
        };

        protected override string ProcessValues(JToken jsonValue, string editorAlias, Func<JObject, IContentType, JObject> GetPropertiesMethod)
        {

            if (jsonValue is JArray elements)
            {
                foreach (var item in elements.Cast<JObject>())
                {
                    var itemValue = item.Value<JObject>("value");
                    if (itemValue == null) continue;

                    var doctype = GetDocTypeByKey(item, "elementType");
                    if (doctype == null) continue;

                    GetImportProperties(itemValue, doctype);
                }
                return JsonConvert.SerializeObject(elements);
            }

            return JsonConvert.SerializeObject(jsonValue);

        }

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var stringValue = GetValueAs<string>(value);

            if (stringValue.TryParseValidJsonString(out JArray elements) is false)
                return Enumerable.Empty<uSyncDependency>();

            if (elements == null || !elements.Any())
                return Enumerable.Empty<uSyncDependency>();

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
