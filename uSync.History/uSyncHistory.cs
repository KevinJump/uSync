using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uSync.BackOffice.Models;

namespace uSync.History
{
    public class uSyncHistory : ISyncAddOn
    {
        public string Name => "_uSync History";

        public string Version => typeof(uSyncHistory).Assembly.GetName().Version?.ToString(3) ?? "16.1.0";

        public string Icon => "icon-history";

        public string View => "/App_Plugins/uSyncHistory/dashboard.html";

        public string Alias => "uSyncHistory";

        public string DisplayName => "History";

        public int SortOrder => 20;
    }
}
