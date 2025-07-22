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
        var attempt = value.TryConvertTo<int>();
        if (attempt.Success is false) return base.GetExportValue(value, editorAlias);

        var group = _memberGroupService.GetById(attempt.Result);
        return group?.Name ?? base.GetExportValue(value, editorAlias);
    }

    /// <summary>
    ///  Import: take the name of the group, and return the id of the group.
    /// </summary>
    public override string GetImportValue(string value, string editorAlias)
    {
        if (string.IsNullOrEmpty(value)) return base.GetImportValue(value, editorAlias);

        var group = _memberGroupService.GetByName(value);
        return group?.Id.ToString() ?? base.GetImportValue(value, editorAlias);
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
        var attempt = value.TryConvertTo<int>();
        if (attempt.Success is false) return base.GetDependencies(value, editorAlias, flags);

        var group = _memberGroupService.GetById(attempt.Result);
        if (group is null) return base.GetDependencies(value, editorAlias, flags);

        return new uSyncDependency
        {
            Flags = DependencyFlags.None,
            Name = group.Name,
            Udi = new GuidUdi(UdiEntityType.MemberGroup, group.Key),
            Level = 0,
            Order = DependencyOrders.OrderFromEntityType(UdiEntityType.MemberGroup)
        }.AsEnumerableOfOne();
    }
}
