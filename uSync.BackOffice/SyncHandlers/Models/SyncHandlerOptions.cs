
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  options that define how we define a handler 
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
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

        /// <summary>
        ///  include handlers that are by default disabled 
        /// </summary>
        public bool IncludeDisabled { get; set; } = false; 

        /// <summary>
        /// Default constructor
        /// </summary>
        public SyncHandlerOptions() { }

        /// <summary>
        /// Construct Options for a given set
        /// </summary>
        public SyncHandlerOptions(string setName)
            : this()
        {
            this.Set = setName;
        }

        /// <summary>
        /// Construct options with set and handler action set.
        /// </summary>
        public SyncHandlerOptions(string setName, HandlerActions action)
            : this()
        {
            this.Set = setName;
            this.Action = action;
        }
    }


}
