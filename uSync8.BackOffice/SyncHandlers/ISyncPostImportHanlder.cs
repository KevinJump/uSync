using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    /// <summary>
    ///  handlers that also need to be called at the end (when everything has been processed)
    ///  
    ///  examples of this ? dataTypes that reference docTypes - because docTypes comes in after dataTypes
    /// </summary>
    public interface ISyncPostImportHandler
    {
        IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config);
    }
}
