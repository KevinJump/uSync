using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Umbraco.Core;

namespace uSync8.BackOffice.Configuration
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncSettings
    {
        public string RootFolder { get; set; } = "~/uSync/v8/";

        public bool UseFlatStructure { get; set; } = true;
        public bool UseGuidNames { get; set; } = false;
        public bool BatchSave { get; set; } = false;

        public bool ImportAtStartup { get; set; } = false;
        public bool ExportAtStartup { get; set; } = false;
        public bool ExportOnSave { get; set; } = true;

        public string DefaultSet { get; set; } = "default";

        public IList<HandlerSet> HandlerSets { get; set; } = new List<HandlerSet>();
        //public List<HandlerSettings> Handlers { get; set; } = new List<HandlerSettings>();

        public bool ReportDebug { get; set; } = false;

        public bool AddOnPing { get; set; } = true;

        public bool RebuildCacheOnCompletion { get; set; } = false;

        public HandlerSet DefaultHandlerSet()
            => this.HandlerSets.Where(x => x.Name.InvariantEquals(this.DefaultSet)).FirstOrDefault();
    }

    public class HandlerSet
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }

        public IList<HandlerSettings> Handlers { get; set; } = new List<HandlerSettings>();
    }

}
