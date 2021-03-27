using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace uSync8.BackOffice.Models
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncHandlerView
    {
        public int Status { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Icon { get; set; }
        public string Group { get; set; }
    }
}
