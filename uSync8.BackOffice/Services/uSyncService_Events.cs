using System.Collections.Generic;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{
    public delegate void uSyncBulkEventHandler(uSyncBulkEventArgs e);

    public partial class uSyncService
    {
        public static event SyncUpdateCallback UpdateMsg;
        public static event SyncEventCallback EventMsg;

        public static event uSyncBulkEventHandler ImportStarting;
        public static event uSyncBulkEventHandler ImportComplete;

        public static event uSyncBulkEventHandler ExportStarting;
        public static event uSyncBulkEventHandler ExportComplete;

        public static event uSyncBulkEventHandler ReportStarting;
        public static event uSyncBulkEventHandler ReportComplete;

        internal static void fireBulkStarting(uSyncBulkEventHandler eventHandler)
        {
            eventHandler?.Invoke(new uSyncBulkEventArgs());
        }

        internal static void fireBulkComplete(uSyncBulkEventHandler eventHandler, IEnumerable<uSyncAction> actions)
        {
            eventHandler?.Invoke(new uSyncBulkEventArgs()
            {
                Actions = actions
            });
        }

        public static void FireUpdateMsg(string message, int count, int total)
        {
            UpdateMsg?.Invoke(message, count, total);
        }

        public static void FireEventMsg(SyncProgressSummary summary)
        {
            EventMsg?.Invoke(summary);
        }
    }

    public class uSyncBulkEventArgs
    {
        public IEnumerable<uSyncAction> Actions { get; set; }
    }
}
