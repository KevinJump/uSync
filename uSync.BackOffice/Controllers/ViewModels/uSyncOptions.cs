using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace uSync.BackOffice.Controllers
{
    /// <summary>
    /// Options passed to Import/Export methods by JS calls
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncOptions
    {
        /// <summary>
        ///  SignalR Hub client id
        /// </summary>
        [DataMember(Name = "clientId")]
        public string ClientId { get; set; }

        /// <summary>
        /// Force the import (even if no changes detected)
        /// </summary>
        [DataMember(Name = "force")]
        public bool Force { get; set; }

        /// <summary>
        /// Make the export clean the folder before it starts 
        /// </summary>
        [DataMember(Name = "clean")]
        public bool Clean { get; set; }

        /// <summary>
        /// Name of the handler group to perfom the actions on
        /// </summary>
        [DataMember(Name = "group")]
        public string Group { get; set; }
    }
}
