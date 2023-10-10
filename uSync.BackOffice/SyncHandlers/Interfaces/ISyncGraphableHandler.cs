using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  graphable handler - handler can use a Topological graph
    ///  to sort items into the most efficient order 
    /// </summary>
    public interface ISyncGraphableHandler
    {
        /// <summary>
        ///  return
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IEnumerable<Guid> GetGraphIds(XElement node);
    }
}
