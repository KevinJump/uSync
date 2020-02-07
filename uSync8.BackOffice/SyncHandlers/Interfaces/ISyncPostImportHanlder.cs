﻿using System.Collections.Generic;

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
        /// <summary>
        ///  Process items again once all other handlers have performed their import
        /// </summary>
        /// <remarks>
        ///  Some handlers require that import actions are performed after all other handlers have been
        ///  processed.
        ///  
        ///  the prime example for this is a datatype that refrences doctypes. Datatypes are required 
        ///  to be imported before doctypes, but then the post import step has to run so the datatype 
        ///  can refrence the doctypes that may not have been there first time around.
        /// </remarks>
        /// <param name="folder">Folder to use for import</param>
        /// <param name="actions">List of actions containing items that require post import processing</param>
        /// <param name="config">Handler settings to use for processing</param>
        /// <returns>List of actions detailing post import changes</returns>
        IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config);
    }
}
