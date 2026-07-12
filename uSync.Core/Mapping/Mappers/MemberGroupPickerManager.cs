using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.Core.Mapping.Mappers;
public class MemberGroupPickerMapper : SyncValueMapperBase, ISyncMapper
{
    private readonly IMemberGroupService _memberGroupService;

    public MemberGroupPickerMapper(
        IEntityService entityService,
        IMemberGroupService memberGroupService)
        : base(entityService)
    {
        _memberGroupService = memberGroupService;
    }

    public override string Name => "Member Group Picker Mapper";
    public override string[] Editors => [
        Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.MemberGroupPicker
    ];

    /// <summary>
    ///  Export: take the int value that is stored , but return the name of the group as the value.
    /// </summary>
    public override async Task<string?> GetExportValueAsync(object value, string editorAlias)
    {
        if (value.TryGetValueAs<string>(out var stringValue) is false)
            return await base.GetExportValueAsync(value, editorAlias);

        var values = stringValue.ToDelimitedList().ConvertItems<int>();

        var groups = new List<string>();

        foreach (var item in values)
        {
            if (item <= 0) continue;

            // do this via the entity service, saves us calling the full service to get the name. 
            var entity = entityService.Get(item, Umbraco.Cms.Core.Models.UmbracoObjectTypes.MemberGroup);
            if (entity is null) continue;
            groups.Add(entity.Name ?? string.Empty);
        }

        return string.Join(",", groups);
    }

    /// <summary>
    ///  Import: take the name of the group, and return the id of the group.
    /// </summary>
    public override async Task<string?> GetImportValueAsync(string value, string editorAlias, SyncSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value))
            return await base.GetImportValueAsync(value, editorAlias, options);

        var items = value.ToDelimitedList();

        var values = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;

            // otherwise, we assume its a group name and try to find the group by name
            var group = await _memberGroupService.GetByNameAsync(item);
            if (group is not null)
                values.Add(group.Id.ToString());
        }

        return string.Join(",", values);
    }

    /// <summary>
    ///  Dependency check, if the group is picked then return a dependency for the group, so it can be synced.
    /// </summary>
    public override async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        // if we are not including dependencies, then return nothing
        if (flags.HasFlag(DependencyFlags.IncludeDependencies) is false)
            return Enumerable.Empty<uSyncDependency>();

        // get the int value and load the group
        if (value.TryGetValueAs<string>(out var stringValue) is false)
            return await base.GetDependenciesAsync(value, editorAlias, flags);

        var values = stringValue.ToDelimitedList().ConvertItems<int>();

        var dependencies = new List<uSyncDependency>();

        foreach (var item in values)
        {
            if (string.IsNullOrWhiteSpace(item.ToString())) continue;

            var group = entityService.Get(item, Umbraco.Cms.Core.Models.UmbracoObjectTypes.MemberGroup);
            if (group is not null)
            {
                dependencies.Add(new uSyncDependency
                {
                    Flags = DependencyFlags.None,
                    Name = group.Name ?? group.Id.ToString(),
                    Udi = new GuidUdi(UdiEntityType.MemberGroup, group.Key),
                    Level = 0,
                    Order = DependencyOrders.OrderFromEntityType(UdiEntityType.MemberGroup)
                });
            }
        }

        return dependencies;
    }
}
