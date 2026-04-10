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

        public string Version => typeof(uSyncHistory).Assembly.GetName().Version?.ToString(3) ?? "17.0";

        public string Icon => "icon-history";

        public string View => "";

        public string Alias => "uSyncHistory";

        public string DisplayName => "History";

        public int SortOrder => 20;
    }

    internal class SyncHistoryConstants
    {
        public const string ApiName = "uSync.History";
        public const string AppName = "uSync.History";
        public const string DisplayName = "uSync History";
        public const string PlugnPath = "/App_Plugins/uSync.History/";

    }
}
