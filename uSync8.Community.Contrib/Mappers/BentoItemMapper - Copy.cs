using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Core;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core;
using uSync8.Core.Dependency;

namespace uSync8.Community.Contrib.Mappers
{
    public class BentoStackMapper : BentoItemMapper, ISyncMapper
    {
        private readonly string docTypeAliasValue = "contentTypeAlias";

        public BentoStackMapper(IEntityService entityService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService, contentTypeService, dataTypeService)
        { }

        public override string Name => "Bento Stack Mapper";

        public override string[] Editors => new string[]
        {
            "bentostack.editor"
        };

        /// <summary>
        ///  Get any formatted export values. 
        /// </summary>
        /// <remarks>
        ///  for 99% of properties you don't need to go in and get the 
        ///  potential internal values, but we do this on export because
        ///  we want to ensure we trigger formatting of Umbraco.DateTime values
        /// </remarks>

        public override string GetExportValue(object value, string editorAlias)
        {
            if (value == null) return string.Empty;

            var stackJson = GetValueAs<JArray>(value);
            if (stackJson == null) return value.ToString();

            foreach(var stack in stackJson)
            {
                var areas = stack.Value<JArray>("areas");
                if (areas == null) continue;

                foreach(var area in areas)
                {
                    var contentData = area.Value<JObject>("contentData");
                    if (contentData != null)
                    {
                        // embedd mode. 
                        var docType = GetDocType(contentData, this.docTypeAliasValue);
                        if (docType == null) continue;

                        GetExportProperties(contentData, docType);
                    }                 
                }

                return JsonConvert.SerializeObject(stackJson, Formatting.Indented);
            }

            return value.ToString();
        }

        /// <summary>
        ///  Dependency check for bento item
        /// </summary>
        /// <remarks>
        ///  a bento item is a single item, stored in json as a link 
        /// 
        ///  when content is linked, its done via the key. 
        ///  {"id":1161,"key":"0af06fdb-9084-4601-a65f-a9b529e731ad","icon":"icon-brick"}
        /// 
        ///  when the content is embedded its in the contentData and behaves like nested content
        ///  {"id":0,"key":"c1d39dea-b590-4d5c-b213-187c2b303d93","contentData":{"name":"Simple Bento Element","contentTypeAlias":"simpleBentoElement","icon":"icon-document","title":"Some title","link":"umb://document/ca4249ed2b234337b52263cabe5587d1"},"icon":"icon-document"}
        ///  
        /// </remarks>
        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {

            if (value == null) return Enumerable.Empty<uSyncDependency>();

            var stackJson = GetValueAs<JArray>(value);
            if (stackJson == null) return Enumerable.Empty<uSyncDependency>();

            List<uSyncDependency> dependencies = new List<uSyncDependency>();

            foreach (var stack in stackJson)
            {
                var areas = stack.Value<JArray>("areas");
                if (areas == null) continue;

                foreach (var area in areas)
                {
                    if (area is JObject itemValue)
                    {
                        dependencies.AddRange(GetItemDependency(itemValue, flags));
                    }
                }
            }

            return dependencies;
        }
    }
}
