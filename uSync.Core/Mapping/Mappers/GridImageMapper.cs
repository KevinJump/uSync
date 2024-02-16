using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

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
        if (value == null) return [];

        if (value.ToString().TryParseToJsonObject(out var image) is false || image is null)
            return [];
            
        var udiString = image.GetPropertyAsString("udi");
        if (!string.IsNullOrWhiteSpace(udiString))
        {
            var dependency = CreateDependency(udiString, flags);
            if (dependency != null)
                return dependency.AsEnumerableOfOne();
        }

        return [];

    }
}
