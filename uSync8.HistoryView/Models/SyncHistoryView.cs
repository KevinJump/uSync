using System;
using System.Collections.Generic;

using uSync8.BackOffice;

namespace uSync8.HistoryView
{

    public class SyncHistoryView
    {
        public string Action { get; set; } = "Import";
        public string Username { get; set; }

        public string Server { get; set; }

        public DateTime When { get; set; }
        public IList<uSyncAction> Changes { get; set; }
    }
}
