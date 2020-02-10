using System.Collections.Generic;

using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{
    internal delegate void uSyncTriggerEventHandler(uSyncTriggerArgs e);

    /// <summary>
    ///  used to trigger uSync events from within other elements of uSync
    /// </summary>
    internal class uSyncTriggers
    {
        internal static event uSyncTriggerEventHandler DoExport;
        internal static event uSyncTriggerEventHandler DoImport;

        /// <summary>
        ///  trigger an export of an item programatically.
        /// </summary>
        /// <param name="folder">folder to use for export</param>
        /// <param name="entityTypes">entity types to trigger export for</param>
        /// <param name="options">handler options to use for handlers</param>
        public static void TriggerExport(string folder, IEnumerable<string> entityTypes, SyncHandlerOptions options)
        {
            DoExport?.Invoke(new uSyncTriggerArgs()
            {
                EntityTypes = entityTypes,
                Folder = folder,
                HandlerOptions = options
            });
        }

        public static void TriggerImport(string folder, IEnumerable<string> entityTypes, SyncHandlerOptions options)
        {
            DoImport?.Invoke(new uSyncTriggerArgs()
            {
                EntityTypes = entityTypes,
                Folder = folder,
                HandlerOptions = options
            });
        }


    }

    internal class uSyncTriggerArgs
    {
        public string Folder { get; set; }

        public IEnumerable<string> EntityTypes { get; set; }

        public SyncHandlerOptions HandlerOptions { get; set; }

    }
}
