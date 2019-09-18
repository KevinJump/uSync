using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]

    public class ExtendedHandlerConfigPair
    {
        public ISyncExtendedHandler Handler { get; set; }
        public HandlerSettings Settings { get; set; }

    }
}
