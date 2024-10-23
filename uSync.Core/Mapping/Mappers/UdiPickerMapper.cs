using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

/// <summary>
///  Mapper for anything that just stores a single or 
///  multiple Udis in a Comma Separated list 
/// </summary>
/// <remarks>
///  These ids can be of any type the base class works 
///  out the dependency order type based on the Udis 
///  
///  We are not supporting partial content imports, where 
///  content that this picker links to may not be in the site
///  to do this we would need to map the UDI to something 
///  even more generic like a path. 
/// </remarks>
[NullableMapper]
public class UdiPickerMapper : SyncValueMapperBase, ISyncMapper
{
    public UdiPickerMapper(IEntityService entityService) : base(entityService)
    {
    }

    public override string Name => "Content Picker Mapper";

    public override string[] Editors => [
        Constants.PropertyEditors.Aliases.ContentPicker,
        // Constants.PropertyEditors.Aliases.MediaPicker,
        Constants.PropertyEditors.Aliases.MultiNodeTreePicker,
        Constants.PropertyEditors.Aliases.MemberPicker
    ];

    public override Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            if (value is not null)
            {
                var udiStrings = value.ToString()?.ToDelimitedList() ?? [];
                return CreateDependencies(udiStrings, flags);
            }

            return [];
        });
    }
}
