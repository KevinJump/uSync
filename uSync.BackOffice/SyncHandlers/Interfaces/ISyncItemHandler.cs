using System.Collections.Generic;
using System.Xml.Linq;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  Item handler allows you to import individual things, gives the 
    ///  calling processes much more control over the flow and how each
    /// </summary>
    public interface ISyncItemHandler 
    {
        IEnumerable<uSyncAction> ReportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options);


        /// <summary>
        ///  Import from a single node. 
        /// </summary>
        IEnumerable<uSyncAction> ImportElement(XElement node, string filename, HandlerSettings settings, uSyncImportOptions options);

        /// <summary>
        ///  Import the second pass of an item.
        /// </summary>
        IEnumerable<uSyncAction> ImportSecondPass(uSyncAction action, HandlerSettings settings, uSyncImportOptions options);

    }
}
