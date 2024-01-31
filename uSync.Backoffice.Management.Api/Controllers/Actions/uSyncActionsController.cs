using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.SyncHandlers;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class uSyncActionsController : uSyncControllerBase
{
    [HttpGet("Actions")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<SyncActionGroup>), 200)]
    public async Task<List<SyncActionGroup>> GetActions()
    {
        var defaultButtons = new List<SyncActionButton>
        {
            new SyncActionButton
            {
                Key = HandlerActions.Report.ToString(),
                Look = "secondary",
                Color = "positive"
            },
            new SyncActionButton
            {
                Key = HandlerActions.Import.ToString(),
                Look = "primary",
                Color = "positive"
            },
            new SyncActionButton
            {
                Key = HandlerActions.Export.ToString(),
                Look = "primary",
                Color = "default"
            }
        };

        List<SyncActionGroup> actions = [
            new SyncActionGroup
            {
                GroupName = "Settings",
                Icon = "icon-settings-alt",
                Key = "settings",
                Buttons = defaultButtons
            },
            new SyncActionGroup
            {
                GroupName = "Content",
                Icon = "icon-documents",
                Key = "content",
                Buttons = defaultButtons
            },
            new SyncActionGroup
            {
                GroupName = "Everything",
                Icon = "icon-paper-plane-alt",
                Key = "all",
                Buttons = defaultButtons
            }
        ];

        return await Task.FromResult(actions);

    }
}
