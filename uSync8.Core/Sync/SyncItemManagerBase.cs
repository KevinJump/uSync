using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;

namespace uSync8.Core.Sync
{
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

                CanExport = meta.CanExport;
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
        public virtual bool CanExport { get; }

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
                Name = EntityType,
                Udi = Udi.Create(EntityType)
            };
    }


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
