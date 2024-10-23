using Umbraco.Cms.Core;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Core.Sync;

/// <summary>
///  base class for ISyncItemManager implimentations
/// </summary>
/// <remarks>
///  the base class leys you use the SyncItemManager attribute
///  to define a SyncItemManager that handles a single entity/tree 
///  combination.
/// </remarks>
public abstract class SyncItemManagerBase
{
    public SyncItemManagerBase()
    {
        var meta = this.GetType().GetCustomAttribute<SyncItemManagerAttribute>(false);
        if (meta != null)
        {
            if (!string.IsNullOrWhiteSpace(meta.EntityType))
                EntityTypes = [meta.EntityType];


            if (!string.IsNullOrEmpty(meta.TreeAlias))
                Trees = [meta.TreeAlias];
        }
    }

    public virtual string[] Trees { get; } = [];
    public virtual string[] EntityTypes { get; } = [];

    protected string EntityType
    {
        get
        {
            if (EntityTypes == null || EntityTypes.Length == 0)
                throw new IndexOutOfRangeException($"{this.GetType().Name} Needs at least one Entity type");

            return EntityTypes[0];
        }
    }


    public virtual bool ShowTreeOptions { get; } = true; ///  show in the tree.


    public virtual SyncTreeType GetTreeType(SyncTreeItem treeItem) => SyncTreeType.Settings;

    /// <summary>
    ///  Get the Root item for the tree (default implementation uses first entity type)
    /// </summary>
    /// <param name="treeItem"></param>
    /// <returns></returns>
    protected virtual SyncLocalItem? GetRootItem(SyncTreeItem treeItem)
        => new(Constants.System.RootString)
        {
            EntityType = EntityType,
            Name = EntityType,
            Udi = Udi.Create(EntityType)
        };

    protected abstract Task<IEnumerable<SyncItem>> GetDescendantsAsync(SyncItem item, DependencyFlags flags);

    /// <summary>
    ///  standard use case, if IncludeChildren flag is set, return this item and all its children.
    ///  if not just return this item. 
    /// </summary>
    public virtual async Task<IEnumerable<SyncItem>> GetItemsAsync(SyncItem item)
    {
        if (item.Flags.HasFlag(DependencyFlags.IncludeChildren))
        {
            var items = new List<SyncItem> { item };
            items.AddRange(await GetDescendantsAsync(item, item.Flags & ~DependencyFlags.IncludeChildren));
            return items;
        }
        else
        {
            return item.AsEnumerableOfOne();
        }
    }

    /// <summary>
    ///  get the sync information needed for an item to appear in uSync.Exporter.
    /// </summary>
    /// <remarks>
    ///  override this if you have a picker that can be used to pick items for exporter.
    /// </remarks>
    public virtual SyncEntityInfo? GetSyncInfo(string entityType) => null;
}

/// <summary>
///  Base class for ISyncItemManager items where the ID for the entity is of Type TIndexType
/// </summary>
/// <remarks>
///  saves you having to write the conversion code in your methods to convert the ID from
///  a string to whatever type the entity uses. 
/// </remarks>
[Obsolete("No longer need to have an indexed based, use SyncItemManagerBase will be removed in v16")]
public abstract class SyncItemManagerIndexBase<TIndexType> : SyncItemManagerBase
{
    [Obsolete("no longer used will be removed in v16")]
    protected virtual Task<SyncLocalItem?> GetLocalEntityAsync(TIndexType id)
        => Task.FromResult(default(SyncLocalItem));


    /// <inheritdoc />
    [Obsolete("no longer used will be removed in v16")]
    public virtual Task<SyncLocalItem?> GetEntityAsync(SyncTreeItem treeItem)
        => Task.FromResult(default(SyncLocalItem));
}
