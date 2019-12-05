using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync8.BackOffice.Controllers
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncOptions
    {
        [DataMember(Name = "clientId")]
        public string ClientId { get; set; }

        [DataMember(Name = "force")]
        public bool Force { get; set; }

        [DataMember(Name = "clean")]
        public bool Clean { get; set; }

        [DataMember(Name = "group")]
        public string Group { get; set; }
    }
}
