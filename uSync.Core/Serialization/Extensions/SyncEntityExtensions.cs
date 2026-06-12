using Umbraco.Cms.Core.Models.Entities;

using uSync.Core.Serialization.Models;

namespace uSync.Core.Serialization.Extensions;

internal static class SyncEntityExtensions
{
    public static int CalculateNodeLevel<TObject>(this TObject item, SyncParentItem? parent)
        where TObject : IEntity
        => parent is null ? 1 : parent.Level + 1;

    public static string CalculateNodePath<TObject>(this TObject item, SyncParentItem? parent)
        where TObject : IEntity
        => parent is null 
            ? string.Join(",", -1, item.Id) 
            : string.Join(",", parent.Path, item.Id);
}
