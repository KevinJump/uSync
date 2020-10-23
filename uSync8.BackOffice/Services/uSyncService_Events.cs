using System.Collections.Generic;
using System.Xml.Linq;

using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Models;

namespace uSync8.BackOffice
{
    public delegate void uSyncBulkEventHandler(uSyncBulkEventArgs e);
    public delegate void uSyncItemEventHandler(uSyncItemEventArgs e);

    public partial class uSyncService
    {
        public static event SyncUpdateCallback UpdateMsg;
        public static event SyncEventCallback EventMsg;

        /// <summary>
        ///  fired before uSync starts an import
        /// </summary>
        public static event uSyncBulkEventHandler ImportStarting;

        /// <summary>
        ///  fired when uSync completes a import 
        /// </summary>
        public static event uSyncBulkEventHandler ImportComplete;

        /// <summary>
        ///  fired before uSync performs an export
        /// </summary>
        public static event uSyncBulkEventHandler ExportStarting;

        /// <summary>
        ///  fired when uSync completes and export 
        /// </summary>
        public static event uSyncBulkEventHandler ExportComplete;

        /// <summary>
        ///  fired before uSync runs an report
        /// </summary>
        public static event uSyncBulkEventHandler ReportStarting;

        /// <summary>
        ///  fired when uSync completes a report
        /// </summary>
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


        /// <summary>
        ///  Before an item is reported. Can be cancelled
        /// </summary>
        public static event uSyncItemEventHandler ReportingItem;

        /// <summary>
        ///  After an item is reported 
        /// </summary>
        public static event uSyncItemEventHandler ReportedItem;

        public static bool FireReportingItem(uSyncItemEventArgs e)
        {
            ReportingItem?.Invoke(e);
            return e.Cancel;
        }

        public static void FireReportedItem(XElement node, ChangeType changeType)
        {
            ReportedItem?.Invoke(new uSyncItemEventArgs
            {
                Item = node, 
                Change = changeType
            });
        }
    }

    public class uSyncBulkEventArgs
    {
        public IEnumerable<uSyncAction> Actions { get; set; }
    }

    public class uSyncItemEventArgs
    {
        public bool Cancel { get; set; }
        public ChangeType Change { get; set; }

        public XElement Item { get; set; }
    }
}
