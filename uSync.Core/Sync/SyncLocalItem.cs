
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System.Collections.Generic;

using Umbraco.Cms.Core;

namespace uSync.Core.Sync
{
    /// <summary>
    ///  Representation of a local item, that can be used to kickoff the UI
    ///  for publishings/exporting.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SyncLocalItem
    {
        /// <summary>
        ///  Internal ID for the item
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///  Display name for the item
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  Umbraco UDI value that identifies the item.
        /// </summary>
        // [JsonConverter(typeof(UdiJsonConverter))]
        public Udi Udi { get; set; } 

        /// <summary>
        ///  Umbraco/Custom EntityType name
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        ///  details of any language variants
        /// </summary>
        /// <remarks>
        ///  when variants are present the user can be presented with 
        ///  the option of what languages they want to sync.
        /// </remarks>
        public Dictionary<string, string> Variants { get; set; }

        /// <summary>
        ///  Syncing of this item requires that the files be synced. 
        ///  e.g if this is a template, we sync the files. because templates
        ///  need files, and they might need the partial views/css/etc.
        /// </summary>
        /// <remarks>
        ///  this value is not yet supported - reserved for future use.
        /// </remarks>
        public bool RequiresFiles { get; set; }

        public SyncLocalItem() {}

        public SyncLocalItem(string id) : this()
        {
            Id = id;
        }

        public SyncLocalItem(int id) : this()
        {
            Id = id.ToString();
        }
    }

}
