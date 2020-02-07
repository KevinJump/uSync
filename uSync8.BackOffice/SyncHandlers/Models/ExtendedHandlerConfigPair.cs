using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    /// <summary>
    ///  Handler and Handler Config Object
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class ExtendedHandlerConfigPair
    {
        /// <summary>
        ///  uSync Handler Item
        /// </summary>
        public ISyncExtendedHandler Handler { get; set; }

        /// <summary>
        ///  Settings for the handler
        /// </summary>
        public HandlerSettings Settings { get; set; }

    }
}
