using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;

namespace uSync.Backoffice.Management.Api.Extensions;

public static class uSyncActionExtensions
{
    public static uSyncActionView ToActionView(this uSyncAction action)
    {
        var msg = string.IsNullOrWhiteSpace(action.Message) is false
            ? action.Message
            : string.IsNullOrWhiteSpace(action.Exception?.Message) is false
                ? action.Exception.Message
                : "An error message would go here, it could be quite long, but we'll just truncate it for now.";

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