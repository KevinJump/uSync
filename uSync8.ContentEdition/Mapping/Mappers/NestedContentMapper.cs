using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Services;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    public class NestedContentMapper : SyncValueMapperBase, ISyncMapper
    {
        private readonly IContentTypeService contentTypeService;

        public NestedContentMapper(
            IContentTypeService contentTypeService,
            IEntityService entityService) 
            : base(entityService)
        {
            this.contentTypeService = contentTypeService;
        }

        public override string Name => "Nested Content Mapper";

        public override string[] Editors => new string[] {
            "Our.Umbraco.NestedContent",
            "Umbraco.NestedContent"
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

            foreach(var item in nestedJson)
            {
                var docTypeAlias = item["ncContentTypeAlias"].ToString();

                if (flags.HasFlag(DependencyFlags.IncludeDependencies))
                {
                    var docTypeDep = CreateDocTypeDependency(docTypeAlias, flags);
                    if (docTypeDep != null)
                        dependencies.Add(docTypeDep);
                }
            }

            return dependencies;
        }

        private uSyncDependency CreateDocTypeDependency(string alias, DependencyFlags flags)
        {
            var item = contentTypeService.Get(alias);
            if (item != null)
            {
                return new uSyncDependency()
                {
                    Name = item.Name,
                    Udi = item.GetUdi(),
                    Order = DependencyOrders.ContentTypes,
                    Flags = flags & ~DependencyFlags.IncludeAncestors,
                    Level = item.Level
                };
            }

            return null;
        }
    }
}

