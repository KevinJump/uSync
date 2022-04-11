using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Hubs
{
    /// <summary>
    /// update message sent via uSync to client
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncUpdateMessage
    {
        /// <summary>
        /// string message to display
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///  nubmer of items processed
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        ///  total number of items we expect to process
        /// </summary>
        public int Total { get; set; }
    }
}
