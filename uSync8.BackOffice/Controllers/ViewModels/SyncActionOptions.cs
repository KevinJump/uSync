using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync8.BackOffice.Controllers
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncActionOptions
    {
        public string ClientId { get; set; }
        public string Handler { get; set; }
        public bool Force { get; set; }

        public IEnumerable<uSyncAction> Actions { get; set; }

    }
}
