using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping
{
    public class BlockListMapper : SyncNestedJsonValueMapperBase, ISyncMapper
    {
        private readonly string docTypeKeyAlias = "contentTypeKey"; //  BlockEditorPropertyEditor.ContentTypeKeyPropertyKey;
        private readonly string[] contentBlocks = new string[]
        {
            "contentData", "settingsData"
        };

        public BlockListMapper(IEntityService entityService,
            Lazy<SyncValueMapperCollection> mapperCollection,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService, mapperCollection, contentTypeService, dataTypeService)
        { }

        public override string Name => "Block List/Grid Mapper";

        public override string[] Editors => new string[]
        {
            Constants.PropertyEditors.Aliases.BlockList,
            "Umbraco.BlockGrid"
        };

        protected override JToken GetImportProperty(object value)
        {
            if (value == null) return null;

            var stringValue = value.GetValueAs<string>();
            if (stringValue == null || !stringValue.DetectIsJson())
                return stringValue;

            // we have to get the json, the serialize the json,
            // this is to make sure we don't serizlize any formatting
            // (like indented formatting). because that would 
            // register changes that are not there.
            var b = JsonConvert.SerializeObject(value.GetJTokenFromObject(), Formatting.None);

            return b;
        }


        protected override JToken GetExportProperty(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.DetectIsJson()) return value;
            return value.GetJsonTokenValue();
        }

        protected override string ProcessValues(JToken jsonValue, string editorAlias, Func<JObject, IContentType, JObject> GetPropertiesMethod)
        {
            if (jsonValue is JObject jObjectValue)
            {
                foreach (var block in contentBlocks)
                {
                    if (jObjectValue.ContainsKey(block))
                    {
                        var contentData = jsonValue.Value<JArray>(block);
                        if (contentData == null) continue;

                        foreach (var item in contentData.Cast<JObject>())
                        {
                            var doctype = GetDocTypeByKey(item, docTypeKeyAlias);
                            if (doctype == null) continue;

                            GetPropertiesMethod(item, doctype);
                        }
                    }
                }

                return JsonConvert.SerializeObject(jObjectValue, Formatting.Indented);
            }

            return JsonConvert.SerializeObject(jsonValue, Formatting.Indented);
        }


        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var jsonValue = GetJsonValue(value);
            if (jsonValue == null) return Enumerable.Empty<uSyncDependency>();


            var dependencies = new List<uSyncDependency>();

            // format of block data.
            // { "layout" : {}, "contentData": {}, "settingsData": {} }

            foreach (var block in contentBlocks)
            {
                // contentData is the thing we need to inspect. 
                if (jsonValue.ContainsKey(block))
                {
                    var contentData = jsonValue.Value<JArray>(block);
                    if (contentData != null)
                    {
                        foreach (var contentItem in contentData.Cast<JObject>())
                        {
                            var contentType = GetDocTypeByKey(contentItem, this.docTypeKeyAlias);
                            if (contentType != null)
                            {
                                dependencies.AddNotNull(CreateDocTypeDependency(contentType, flags));
                                dependencies.AddRange(this.GetPropertyDependencies(contentItem, contentType, flags));
                            }
                        }
                    }
                }
            }

            return dependencies;
        }
    }
}
