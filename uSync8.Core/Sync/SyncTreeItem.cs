using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace uSync8.Core.Sync
{
    /// <summary>
    ///  representation of an item from an Umbraco tree.
    /// </summary>
    /// <remarks>
    ///  this is the basic element that allows uSync to locate the 
    ///  correct ISyncItemManager for an item on the tree and work
    ///  out if we show a menu, sync, export etc.
    /// </remarks>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SyncTreeItem
    {
        /// <summary>
        ///  Id of the item.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///  the treeAlias the item is in
        /// </summary>
        public string TreeAlias { get; set; }
       
        /// <summary>
        ///  the section the item is in.
        /// </summary>
        public string SectionAlias { get; set; }
    }

}
