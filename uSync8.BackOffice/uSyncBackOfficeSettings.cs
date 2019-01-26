using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice
{
    public class uSyncBackOfficeSettings
    {
        public string rootFolder => "~/uSync/v8/";

        public bool UseFlatStructure { get; set; } = true;       
        public bool ImportAtStartup { get; set; } = false;

        public bool ExportAtStartup { get; set; } = false;
        public bool ExportOnSave { get; set; } = true;

        public void LoadSettings()
        {
        }
    }
}
