using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice;

namespace uSync.Backoffice.Management.Api.Extensions;

public static class uSyncActionExtensions
{
    public static uSyncActionView ToActionView(this uSyncAction action)
        => new uSyncActionView
        {
            Key = action.Key,
            Name = action.Name,
            ItemType = action.ItemType,
            Change = action.Change,
            Success = action.Success,
            Details = action.Details?.ToList() ?? []

        };
}