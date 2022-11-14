using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Controllers
{
    /// <summary>
    /// Options to tell uSync how to process an action
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SyncActionOptions
    {
        /// <summary>
        /// SignalR client id 
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Name of the handler to use for the action
        /// </summary>
        public string Handler { get; set; }

        /// <summary>
        /// Should the action be forced 
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Set to use when processing the action
        /// </summary>
        public string Set { get; set; }

        /// <summary>
        /// SyncActions to use as the source for all individual actions
        /// </summary>
        public IEnumerable<uSyncAction> Actions { get; set; }

        /// <summary>
        ///  the folder (has to be in the uSync folder) you want to import
        /// </summary>
        public string Folder { get; set; }

    }
}
