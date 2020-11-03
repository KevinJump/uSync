using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Core.Models.Entities;

using uSync8.BackOffice.Configuration;
using uSync8.Core.Models;
using uSync8.Core.Serialization;

namespace uSync8.BackOffice.SyncHandlers
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

        /// <summary>
        ///  clean up handler, deregister events etc.
        /// </summary>
        /// <param name="settings"></param>
        void Terminate(HandlerSettings settings);

    }
}
