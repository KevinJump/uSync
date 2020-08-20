using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Owin.Security.DataProtection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.Core;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    public class BlockListMapper : SyncNestedValueMapperBase, ISyncMapper
    {
        private readonly string docTypeKeyAlias = "contentTypeKey";

        public BlockListMapper(IEntityService entityService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService, contentTypeService, dataTypeService)
        {}

        public override string Name => "Block List Mapper";

        public override string[] Editors => new string[]
        {
            "Umbraco.BlockList"
        };

        public override string GetExportValue(object value, string editorAlias)
        {
            if (value == null) return string.Empty;

            var jsonValue = GetJsonValue(value);
            if (jsonValue == null) return value.ToString();

            if (jsonValue.ContainsKey("contentData"))
            {
                var contentData = jsonValue.Value<JArray>("contentData");
                if (contentData == null) return value.ToString();

                foreach (var item in contentData.Cast<JObject>())
                {
                    var doctype = GetDocTypeByKey(item, docTypeKeyAlias);
                    if (doctype == null) continue;

                    GetExportProperties(item, doctype);
                }

                return JsonConvert.SerializeObject(jsonValue, Formatting.Indented);
            }

            return value.ToString();

        }

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var jsonValue = GetJsonValue(value);
            if (jsonValue == null) return Enumerable.Empty<uSyncDependency>();


            var dependencies = new List<uSyncDependency>();

            // format of block data.
            // { "layout" : {}, "contentData": {}, "settingsData": {} }

            // contentData is the thing we need to inspect. 
            if (jsonValue.ContainsKey("contentData"))
            {
                var contentData = jsonValue.Value<JArray>("contentData");
                if (contentData != null)
                {
                    foreach (var contentItem in contentData.Cast<JObject>())
                    {
                        var contentType = GetDocTypeByKey(contentItem, "contentTypeKey");
                        if (contentType != null)
                        {
                            dependencies.AddNotNull(CreateDocTypeDependency(contentType, flags));
                            dependencies.AddRange(this.GetPropertyDependencies(contentItem, contentType, flags));
                        }
                    }
                }
            }

            return dependencies;
        }
    }
}
