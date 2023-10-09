using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace uSync.BackOffice.SyncHandlers
{
    public interface ISyncGraphableHandler
    {
        public IEnumerable<Guid> GetGraphIds(XElement node);
    }
}
