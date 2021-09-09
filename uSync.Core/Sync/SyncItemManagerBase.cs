using System;
using System.Collections.Generic;

using Umbraco.Cms.Core;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Core.Sync
{
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
                    EntityTypes = new[] { meta.EntityType };


                if (!string.IsNullOrEmpty(meta.TreeAlias))
                    Trees = new[] { meta.TreeAlias };
            }
        }

        public virtual string[] EntityTypes { get; }

        protected string EntityType
        {
            get
            {
                if (EntityTypes == null || EntityTypes.Length == 0)
                    throw new IndexOutOfRangeException($"{this.GetType().Name} Needs at least one Entity type");

                return EntityTypes[0];
            }
        }

        public virtual string[] Trees { get; }

        public virtual bool ShowTreeOptions { get; } = true; ///  show in the tree.


        public virtual SyncTreeType GetTreeType(SyncTreeItem treeItem) => SyncTreeType.Settings;

        /// <summary>
        ///  Get the Root item for the tree (default implimentation uses first entity type)
        /// </summary>
        /// <param name="treeItem"></param>
        /// <returns></returns>
        protected virtual SyncLocalItem GetRootItem(SyncTreeItem treeItem)
            => new SyncLocalItem(Constants.System.RootString)
            {
                EntityType = EntityType,
                Name = EntityType,
                Udi = Udi.Create(EntityType)
            };

        protected abstract IEnumerable<SyncItem> GetDecendants(SyncItem item, DependencyFlags flags);

        /// <summary>
        ///  standard use case, if IncludeChildren flag is set, return this item and all its children.
        ///  if not just return this item. 
        /// </summary>
        public virtual IEnumerable<SyncItem> GetItems(SyncItem item)
        {
            if (item.Flags.HasFlag(DependencyFlags.IncludeChildren))
            {
                var items = new List<SyncItem> { item };
                items.AddRange(GetDecendants(item, item.Flags & ~DependencyFlags.IncludeChildren));
                return items;
            }
            else
            {
                return item.AsEnumerableOfOne();
            }
        }

        /// <summary>
        ///  get the sync infomation needed for an item to appear in uSync.Exporter.
        /// </summary>
        /// <remarks>
        ///  override this if you have a picker that can be used to pick items for exporter.
        /// </remarks>
        public virtual SyncEntityInfo GetSyncInfo(string entityType) => null;
    }

    /// <summary>
    ///  Base class for ISyncItemManager items where the ID for the entity is of Type TIndexType
    /// </summary>
    /// <remarks>
    ///  saves you having to write the convertion code in your methods to conver the ID from
    ///  a string to whatever type the enity uses. 
    /// </remarks>
    public abstract class SyncItemManagerIndexBase<TIndexType> : SyncItemManagerBase
    {
        protected abstract SyncLocalItem GetLocalEntity(TIndexType id);

        public virtual SyncLocalItem GetEntity(SyncTreeItem treeItem)
        {
            if (treeItem.IsRoot()) return GetRootItem(treeItem);

            var attempt = treeItem.Id.TryConvertTo<TIndexType>();
            if (attempt.Success)
                return GetLocalEntity(attempt.Result);

            return null;
        }
    }
}
