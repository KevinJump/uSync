using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;

using static Umbraco.Cms.Core.Constants;

namespace uSync.Core.Mapping.Mappers;

public class MemberGroupPickerMapper : SyncValueMapperBase, ISyncMapper
{
    private readonly IMemberGroupService _memberGroupService;

    public MemberGroupPickerMapper(IEntityService entityService, IMemberGroupService memberGroupService) : base(entityService)
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
    public override string GetExportValue(object value, string editorAlias)
    {
        var attempt = value.TryConvertTo<string>();
        if (attempt.Success is false || attempt.Result is null)
            return base.GetExportValue(value, editorAlias);

        var values = ConvertToList<int>(attempt.Result.ToDelimitedList());

        var groups = new List<string>();

        foreach (var item in values)
        {
            if (item <= 0) continue;
            // get the group by id
            var group = _memberGroupService.GetById(item);
            if (group is not null)
                groups.Add(group?.Name ?? string.Empty);
        }

        return string.Join(",", groups);
    }

    /// <summary>
    ///  Import: take the name of the group, and return the id of the group.
    /// </summary>
    public override string GetImportValue(string value, string editorAlias)
    {
        if (string.IsNullOrEmpty(value))
            return base.GetImportValue(value, editorAlias);

        var items = value.ToDelimitedList();

        var values = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;

            // otherwise, we assume its a group name and try to find the group by name
            var group = _memberGroupService.GetByName(item);
            if (group is not null)
                values.Add(group.Id.ToString());
        }

        return string.Join(",", values);
    }

    /// <summary>
    ///  Dependency check, if the group is picked then return a dependency for the group, so it can be synced.
    /// </summary>
    public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
    {
        // if we are not including dependencies, then return nothing
        if (flags.HasFlag(DependencyFlags.IncludeDependencies) is false)
            return Enumerable.Empty<uSyncDependency>();

        // get the int value and load the group
        var attempt = value.TryConvertTo<string>();
        if (attempt.Success is false || attempt.Result is null)
            return base.GetDependencies(value, editorAlias, flags);

        var values = ConvertToList<int>(attempt.Result.ToDelimitedList());
        var dependencies = new List<uSyncDependency>();

        foreach (var item in values)
        {
            if (string.IsNullOrWhiteSpace(item.ToString())) continue;

            var group = _memberGroupService.GetById(item);
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

    private static IEnumerable<T> ConvertToList<T>(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            var attempt = item.TryConvertTo<T>();
            if (attempt.Success && attempt.Result is not null)
            {
                yield return attempt.Result;
            }
        }
    }
}
