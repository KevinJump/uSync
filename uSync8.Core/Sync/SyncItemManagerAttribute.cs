using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.Core.Sync
{
    /// <summary>
    ///  Setup your sync item manager (single tree/single entity)
    /// </summary>
    public class SyncItemManagerAttribute : Attribute
    {
        /// <summary>
        ///  the base entity type that this item works for.
        /// </summary>
        public string EntityType { get; private set; }

        /// <summary>
        ///  the alias of the tree in the UI
        /// </summary>
        public string TreeAlias { get; private set; }

        public SyncItemManagerAttribute(string entityType)
        {
            EntityType = entityType;
        }

        public SyncItemManagerAttribute(string entityType, string treeAlias)
        {
            EntityType = entityType;
            TreeAlias = treeAlias;
        }
    }
}
