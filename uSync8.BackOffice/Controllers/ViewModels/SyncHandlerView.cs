
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync8.BackOffice.Controllers
{
    /// <summary>
    ///  view model of a handler, sent to the UI to draw the handler boxes.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncHandlerView
    {
        public bool Enabled { get; set; }
        public int Status { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Icon { get; set; }
        public string Group { get; set; }
    }
}
