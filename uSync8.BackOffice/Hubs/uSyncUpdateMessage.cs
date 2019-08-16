using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync8.BackOffice.Hubs
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncUpdateMessage
    {
        public string Message { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }
    }
}
