using uSync.BackOffice;
using uSync.BackOffice.Models;

namespace uSync.Backoffice.Management.Api.Extensions;

public static class uSyncActionExtensions
{
    [Obsolete("This is for backwards compatibility, use AsActionView instead - will be removed in v19")]
    public static uSyncActionView ToActionView(this uSyncAction action)
    {
        var msg = string.IsNullOrWhiteSpace(action.Message) is false
            ? action.Message
            : string.IsNullOrWhiteSpace(action.Exception?.Message) is false
                ? action.Exception.Message
                : "";

        return new uSyncActionView
        {
            Key = action.Key,
            Name = action.Name,
            Handler = action.HandlerAlias ?? "",
            ItemType = action.ItemType,
            Change = action.Change,
            Success = action.Success,
            Details = action.Details?.ToList() ?? [],
            Message = msg
        };
    }
}