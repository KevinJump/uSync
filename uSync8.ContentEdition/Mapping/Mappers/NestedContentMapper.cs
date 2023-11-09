using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

using uSync8.Core;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    public class NestedContentMapper : SyncNestedValueMapperBase, ISyncMapper
    {
        private readonly string docTypeAliasValue = "ncContentTypeAlias";
        public NestedContentMapper(
            IEntityService entityService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            ILogger logger)
            : base(entityService, contentTypeService, dataTypeService, logger)
        { }

        public override string Name => "Nested Content Mapper";

        public override string[] Editors => new string[] {
            "Our.Umbraco.NestedContent",
            Constants.PropertyEditors.Aliases.NestedContent
        };

        public override string GetImportValue(string value, string editorAlias)
        {
            var nestedJson = GetItemArray(value);
            if (nestedJson == null || !nestedJson.Any()) return value.ToString();

            foreach(var item in nestedJson.Cast<JObject>())
            {
                var docType = GetDocType(item, this.docTypeAliasValue);
                if (docType == null) continue;

                GetImportProperties(item, docType);
            }

            return JsonConvert.SerializeObject(nestedJson, Formatting.Indented);
        }

        public override string GetExportValue(object value, string editorAlias)
        {
            var nestedJson = GetItemArray(value);
            if (nestedJson == null || !nestedJson.Any()) return value.ToString();

            foreach (var item in nestedJson.Cast<JObject>())
            {
                var docType = GetDocType(item, this.docTypeAliasValue);
                if (docType == null) continue;

                GetExportProperties(item, docType);
            }

            return JsonConvert.SerializeObject(nestedJson, Formatting.Indented);
        }

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var nestedJson = GetItemArray(value);
            if (nestedJson == null || !nestedJson.Any())
                return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (var item in nestedJson.Cast<JObject>())
            {
                var docTypeAlias = item[this.docTypeAliasValue].ToString();
                var docType = GetDocType(docTypeAlias);
                if (docType == null) continue;

                if (flags.HasFlag(DependencyFlags.IncludeDependencies))
                {
                    var docTypeDep = CreateDocTypeDependency(docTypeAlias, flags);
                    if (docTypeDep != null)
                        dependencies.Add(docTypeDep);
                }

                dependencies.AddRange(GetPropertyDependencies(item, docType, flags));
            }

            return dependencies;
        }

        private JArray GetItemArray(object value)
        {
            var stringValue = GetValueAs<string>(value);
            if (string.IsNullOrWhiteSpace(stringValue) || !stringValue.DetectIsJson())
                return null;

            var token = JToken.Parse(stringValue);
            switch (token)
            {
                case JArray array: return array;
                case JObject obj: return new JArray(obj);
                default: return null;
            }
        }
    }
}

