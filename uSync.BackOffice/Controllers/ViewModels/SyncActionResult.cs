using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Controllers
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncActionResult
    {

        public SyncActionResult() { }
        public SyncActionResult(IEnumerable<uSyncAction> actions)
        {
            this.Actions = actions;
        }

        public IEnumerable<uSyncAction> Actions { get; set; }
    }
}
