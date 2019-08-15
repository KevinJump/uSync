using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using Umbraco.Core;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice.Configuration
{
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
