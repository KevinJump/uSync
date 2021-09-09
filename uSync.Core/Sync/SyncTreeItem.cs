using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Umbraco.Cms.Core;
using Microsoft.AspNetCore.Http;

namespace uSync.Core.Sync
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

        /// <summary>
        ///  any query strings that might be set on the menu item.
        /// </summary>
        public FormCollection QueryStrings { get; set;  }

    }

    public static class SyncTreeItemExtensions
    {
        /// <summary>
        ///  Is this the root item for the entity 
        /// </summary>
        /// <remarks>
        ///  this method assums the item is following Umbraco conventions for root (i.e "-1")
        /// </remarks>
        public static bool IsRoot(this SyncTreeItem item)
            => item.Id == Constants.System.RootString;
    }

}
