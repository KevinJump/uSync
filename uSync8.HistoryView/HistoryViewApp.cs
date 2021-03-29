
using Umbraco.Web;

using uSync8.BackOffice.Models;

namespace uSync8.HistoryView
{
    public class HistoryViewApp : ISyncAddOn
    {
        public string Name => "_History";

        public string Version => "1.0.0";

        public string Icon => "icon-hourglass";

        public string View => UriUtility.ToAbsolute("/App_Plugins/uSyncHistory/history.html");

        public string Alias => "history";

        public string DisplayName => "History";

        public int SortOrder => 1;
    }
}
