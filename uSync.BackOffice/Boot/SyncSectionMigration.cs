using System.Linq;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;

namespace uSync.BackOffice.Boot;

/// <summary>
///  gives the admin group access to the uSync section, custom sections
///  are not given to any group by default, so without this nobody would see it.
/// </summary>
/// <remarks>
///  runs once, so if someone removes the section from the admin group later we don't put it back.
/// </remarks>
internal class SyncSectionMigration : AsyncMigrationBase
{
    private readonly IUserGroupService _userGroupService;

    public SyncSectionMigration(
        IUserGroupService userGroupService,
        IMigrationContext context) : base(context)
    {
        _userGroupService = userGroupService;
    }

    protected override async Task MigrateAsync()
    {
        IUserGroup? adminGroup = await _userGroupService.GetAsync(Constants.Security.AdminGroupKey);
        if (adminGroup is null) return;
        if (adminGroup.AllowedSections.Contains(uSyncConstants.uSyncSection)) return;

        adminGroup.AddAllowedSection(uSyncConstants.uSyncSection);
        await _userGroupService.UpdateAsync(adminGroup, Constants.Security.SuperUserKey);
    }
}
