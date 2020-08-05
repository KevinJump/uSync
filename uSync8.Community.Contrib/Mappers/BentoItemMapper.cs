using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Core;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core;
using uSync8.Core.Dependency;

namespace uSync8.Community.Contrib.Mappers
{
    public class BentoItemMapper : SyncNestedValueMapperBase, ISyncMapper
    {
        private readonly string docTypeAliasValue = "contentTypeAlias";

        public BentoItemMapper(IEntityService entityService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService, contentTypeService, dataTypeService)
        { }

        public override string Name => "Bento Item Mapper";

        public override string[] Editors => new string[]
        {
            "bentoitem.editor"
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

            var jsonValue = GetJsonValue(value);
            if (jsonValue == null) return value.ToString();

            if (jsonValue.ContainsKey("contentData"))
            {
                var contentValue = jsonValue.Value<JObject>("contentData");
                if (contentValue == null) return value.ToString();

                var docType = GetDocType(contentValue, this.docTypeAliasValue);
                if (docType == null) return value.ToString();

                GetExportProperties(contentValue, docType);

                return JsonConvert.SerializeObject(jsonValue, Formatting.Indented);
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

            var itemValue = GetValueAs<JObject>(value);
            if (itemValue == null) return Enumerable.Empty<uSyncDependency>();

            return GetItemDependency(itemValue, flags);
        }

        protected IEnumerable<uSyncDependency> GetItemDependency(JObject itemValue, DependencyFlags flags)
        {
            if (itemValue == null) return Enumerable.Empty<uSyncDependency>();

            if (itemValue.ContainsKey("contentData"))
            {
                // nested content mode. 
                var contentData = itemValue.Value<JObject>("contentData");
                var docTypeAlias = contentData.Value<string>(this.docTypeAliasValue);
                if (contentData == null || docTypeAlias == null)
                    return Enumerable.Empty<uSyncDependency>();

                var docType = GetDocType(docTypeAlias);
                if (docType == null)
                    return Enumerable.Empty<uSyncDependency>();

                List<uSyncDependency> dependencies = new List<uSyncDependency>();

                var docTypeDependency = CreateDocTypeDependency(docTypeAlias, flags);
                dependencies.AddNotNull(docTypeDependency);

                dependencies.AddRange(GetPropertyDependencies(contentData, docType, flags));

                return dependencies;
            }
            else
            {
                // linked content mode
                var key = itemValue.Value<Guid>("key");
                if (key != null)
                {
                    var udi = GuidUdi.Create(Constants.UdiEntityType.Document, key);
                    return CreateDependency(udi as GuidUdi, flags).AsEnumerableOfOne();
                }
            }

            return Enumerable.Empty<uSyncDependency>();
        }
    }
}
