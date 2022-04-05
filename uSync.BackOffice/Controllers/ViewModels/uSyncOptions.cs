using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Controllers
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
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

        [DataMember(Name = "set")]
        public string Set { get; set; }
    }
}
