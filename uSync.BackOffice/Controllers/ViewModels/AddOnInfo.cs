using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Controllers
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class AddOnInfo
    {
        public string Version { get; set; }

        public string AddOnString { get; set; }
        public List<ISyncAddOn> AddOns { get; set; } = new List<ISyncAddOn>();
    }
}
