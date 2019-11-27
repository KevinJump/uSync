using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Services;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    public class GridImageMapper : SyncValueMapperBase, ISyncMapper
    {
        public GridImageMapper(IEntityService entityService) : base(entityService)
        {
        }

        public override string Name => "Grid Image Mapper";

        public override string[] Editors => new string[] { 
            $"{Constants.PropertyEditors.Aliases.Grid}.media"
        };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            if (value == null) return Enumerable.Empty<uSyncDependency>();

            var image = JsonConvert.DeserializeObject<JObject>(value.ToString());

            var udiString = image.Value<string>("udi");
            if (!string.IsNullOrWhiteSpace(udiString))
            {
                var dependency = CreateDependency(udiString, flags);
                if (dependency != null)
                    return dependency.AsEnumerableOfOne();
            }

            return Enumerable.Empty<uSyncDependency>();

        }
    }
}
