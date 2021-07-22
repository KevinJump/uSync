using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Controllers
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SyncActionOptions
    {
        public string ClientId { get; set; }
        public string Handler { get; set; }
        public bool Force { get; set; }

        public IEnumerable<uSyncAction> Actions { get; set; }

    }
}
