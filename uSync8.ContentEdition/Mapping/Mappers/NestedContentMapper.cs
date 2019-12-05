using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Core;
using Umbraco.Core.Services;

using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    public class NestedContentMapper : SyncNestedValueMapperBase, ISyncMapper
    {
        public NestedContentMapper(
            IEntityService entityService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService)
            : base(entityService, contentTypeService, dataTypeService)
        { }

        public override string Name => "Nested Content Mapper";

        public override string[] Editors => new string[] {
            "Our.Umbraco.NestedContent",
            Constants.PropertyEditors.Aliases.NestedContent
        };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var stringValue = GetValueAs<string>(value);
            if (string.IsNullOrWhiteSpace(stringValue) || !stringValue.DetectIsJson())
                return Enumerable.Empty<uSyncDependency>();

            var nestedJson = JsonConvert.DeserializeObject<JArray>(stringValue);
            if (nestedJson == null || !nestedJson.Any())
                return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (var item in nestedJson.Cast<JObject>())
            {
                var docTypeAlias = item["ncContentTypeAlias"].ToString();

                var docType = contentTypeService.Get(docTypeAlias);
                if (docType == null) continue;

                if (flags.HasFlag(DependencyFlags.IncludeDependencies))
                {
                    var docTypeDep = CreateDocTypeDependency(docTypeAlias, flags);
                    if (docTypeDep != null)
                        dependencies.Add(docTypeDep);
                }

                dependencies.AddRange(GetPropertyDependencies(item,
                    docType.CompositionPropertyTypes, flags));
            }

            return dependencies;
        }
    }
}

