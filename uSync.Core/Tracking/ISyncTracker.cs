using System.Collections.Generic;
using System.Xml.Linq;

using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking
{
    public interface ISyncTrackerBase { }

    public interface ISyncTracker<TObject> : ISyncTrackerBase
    {
        IEnumerable<uSyncChange> GetChanges(XElement node, XElement current, SyncSerializerOptions options);
        IEnumerable<uSyncChange> GetChanges(XElement node, SyncSerializerOptions options);
    }

}
