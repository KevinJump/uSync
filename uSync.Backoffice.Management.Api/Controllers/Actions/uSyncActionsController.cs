using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using uSync.Backoffice.Management.Api.Models;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class uSyncActionsController : uSyncControllerBase
{
    [HttpGet("time")]
    [ProducesResponseType(typeof(string), 200)]
    public string GetTime()
        => DateTime.Now.ToString("s");

    [HttpGet("actions")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(List<SyncActionGroup>), 200)]
    public async Task<List<SyncActionGroup>> GetActions()
    {
        var defaultButtons = new List<SyncActionButton>
        {
            new SyncActionButton
            {
                Key = "report",
                Look = "secondary",
                Color = "positive"
            },
            new SyncActionButton
            {
                Key = "import",
                Look = "primary",
                Color = "positive"
            },
            new SyncActionButton
            {
                Key = "export",
                Look = "primary",
                Color = "default"
            }
        };

        List<SyncActionGroup> actions = [
            new SyncActionGroup
            {
                GroupName = "Settings !",
                Icon = "icon-settings-alt",
                Key = "settings",
                Buttons = defaultButtons
            },
            new SyncActionGroup
            {
                GroupName = "Content !",
                Icon = "icon-documents",
                Key = "settings",
                Buttons = defaultButtons
            },
            new SyncActionGroup
            {
                GroupName = "Everything !",
                Icon = "icon-paper-plane-alt",
                Key = "settings",
                Buttons = defaultButtons
            }
        ];

        return await Task.FromResult(actions);

    }
}
