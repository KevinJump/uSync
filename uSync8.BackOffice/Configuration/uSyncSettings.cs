using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
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

        public bool EnableMissingHandlers { get; set; } = true;
        public List<HandlerSettings> Handlers { get; set; } = new List<HandlerSettings>();

        public bool ReportDebug { get; set; } = false;

        public bool AddOnPing { get; set; } = true;
    }

}
