using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml.Linq;

using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;
using uSync8.Core.Models;

namespace uSync8.BackOffice
{
    public delegate void uSyncBulkEventHandler(uSyncBulkEventArgs e);
    public delegate void uSyncItemEventHandler(uSyncItemEventArgs<XElement> e);
    public delegate void uSyncItemObjectEventHandler(uSyncItemEventArgs<object> e);

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


        /// <summary>
        ///  Before an item is imported can be cancelled
        /// </summary>
        public static event uSyncItemEventHandler ImportingItem;

        /// <summary>
        ///  After an item is imported
        /// </summary>
        public static event uSyncItemEventHandler ImportedItem;

        /// <summary>
        ///  before an item is exported (can be cancelled)
        /// </summary>
        public static event uSyncItemObjectEventHandler ExportingItem;

        /// <summary>
        ///  after an item is exported
        /// </summary>
        public static event uSyncItemEventHandler ExportedItem;


        public static bool FireReportingItem(XElement node)
            => FireItemStartingEvent(ReportingItem, node);

        public static void FireReportedItem(XElement node, ChangeType changeType)
            => FireItemCompletedEvent(ReportedItem, node, changeType);


        public static bool FireImportingItem(XElement node)
            => FireItemStartingEvent(ImportingItem, node);

        public static void FireImportedItem(XElement node, ChangeType changeType)
            => FireItemCompletedEvent(ImportedItem, node, changeType);

        public static bool FireExportingItem(object item)
        {
            if (ExportingItem != null)
            {
                var e = new uSyncItemEventArgs<object> { Item = item };
                ExportingItem?.Invoke(e);
                return !e.Cancel;
            }

            return true;

        }

        public static void FireExportedItem(XElement node, ChangeType changeType)
            => FireItemCompletedEvent(ExportedItem, node, changeType);

        private static bool FireItemStartingEvent(uSyncItemEventHandler eventHandler, XElement node)
        {
            if (eventHandler != null)
            {
                var e = new uSyncItemEventArgs<XElement> { Item = node };
                eventHandler?.Invoke(e);

                // set node back to whatever was set in the event ? 
                node = e.Item;

                return !e.Cancel;
            }

            return true;
        }

        private static void FireItemCompletedEvent(uSyncItemEventHandler eventHandler, XElement node, ChangeType changeType)
        {
            eventHandler?.Invoke(new uSyncItemEventArgs<XElement>
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

    public class uSyncItemEventArgs<TObject>
    {
        public bool Cancel { get; set; }
        public ChangeType Change { get; set; }

        public TObject Item { get; set; }
    }
}
