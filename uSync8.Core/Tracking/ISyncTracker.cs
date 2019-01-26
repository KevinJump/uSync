using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models.Entities;
using uSync8.Core.Models;

namespace uSync8.Core.Tracking
{
    public interface ISyncTracker<TObject>
        where TObject : IEntity
    {
        IEnumerable<uSyncChange> GetChanges(XElement node);
    }
}
