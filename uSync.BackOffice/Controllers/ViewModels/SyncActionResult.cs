using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Controllers
{
    /// <summary>
    /// Result of a series of actions performed by a controller 
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SyncActionResult
    {
        /// <summary>
        /// Construct a new SyncActionResult object
        /// </summary>
        public SyncActionResult() { }

        /// <summary>
        /// Construct a new SyncActionResult object
        /// </summary>
        /// <param name="actions">list of actions to include</param>
        public SyncActionResult(IEnumerable<uSyncAction> actions)
        {
            this.Actions = actions;
        }

        /// <summary>
        /// List of actions performed by process
        /// </summary>
        public IEnumerable<uSyncAction> Actions { get; set; }
    }
}
