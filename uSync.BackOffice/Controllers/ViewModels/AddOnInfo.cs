using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Controllers
{
    /// <summary>
    /// Information about uSync AddOns (displayed in version string)
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AddOnInfo
    {
        /// <summary>
        /// Version of uSync we are running
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Display string for all the add ons installed
        /// </summary>
        public string AddOnString { get; set; }

        /// <summary>
        /// List of all the uSync AddOns installed
        /// </summary>
        public List<ISyncAddOn> AddOns { get; set; } = new List<ISyncAddOn>();
    }
}
