
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.SyncHandlers
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    /// <summary>
    ///  options that define how we define a handler 
    /// </summary>
    public class SyncHandlerOptions
    {
        /// <summary>
        ///  Handler grouping (settings, content, etc) 
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        ///  What action do you want to perform.
        /// </summary>
        public HandlerActions Action { get; set; } = HandlerActions.None;

        /// <summary>
        ///  handler set
        /// </summary>
        public string Set { get; set; } = uSync.Sets.DefaultSet;

        public SyncHandlerOptions() { }

        public SyncHandlerOptions(string setName)
            : this()
        {
            this.Set = setName;
        }

        public SyncHandlerOptions(string setName, HandlerActions action)
            : this()
        {
            this.Set = setName;
            this.Action = action;
        }
    }


}
