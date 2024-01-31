using Asp.Versioning;

using MessagePack.Formatters;

using Microsoft.AspNetCore.Mvc;

using uSync.Backoffice.Management.Api.Models;

namespace uSync.Backoffice.Management.Api.Controllers.Actions;

[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Actions")]
public class uSyncPerformActionController : uSyncControllerBase
{
    private static List<ActionInfo> Actions = [
        new() { ActionName = "languages", Icon = "icon-globe" },
        new() { ActionName = "dictionary", Icon = "icon-book-alt" },
        new() { ActionName = "data types", Icon = "icon-autofill" },
        new() { ActionName = "templates", Icon = "icon-layout" },
        new() { ActionName = "content types", Icon = "icon-item-arrangement" },
        new() { ActionName = "media types", Icon = "icon-thumbnails" },
        new() { ActionName = "member types", Icon = "icon-users" },
        new() { ActionName = "content", Icon = "icon-document" },
        new() { ActionName = "media", Icon = "icon-picture" },
        new() { ActionName = "domains", Icon = "icon-home" },
    ];


    [HttpPost("Perform")]
    [ProducesResponseType(typeof(PerformActionResponse), 200)]
    public object PerformAction(PerformActionRequestModel model)
    {
        var requestId = string.IsNullOrEmpty(model.RequestId)
                ? Guid.NewGuid().ToString() : model.RequestId;


        var completed = model.StepNumber > Actions.Count;

        Thread.Sleep(600);

        var currentActions = new List<ActionInfo>(Actions);

        for (int n = 0; n < currentActions.Count; n++)
        {
            currentActions[n].Completed = model.StepNumber > n + 1 ;
            currentActions[n].Working = model.StepNumber == n + 1;

            if (currentActions[n].Completed)
            {
                currentActions[n].Icon = "icon-check";
            }
        };

        return new PerformActionResponse(
            requestId,
            completed,
            currentActions);
    }
}
