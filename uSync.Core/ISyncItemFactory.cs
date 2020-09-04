using System.Collections.Generic;
using System.Xml.Linq;

using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Core
{
    /// <summary>
    ///  Factory for getting changes or dependencies from an item.
    /// </summary>
    public interface ISyncItemFactory
    {
        // tracking items
        IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>();
        IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, SyncSerializerOptions options);

        // dependency checker items
        IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>();
        IEnumerable<uSyncDependency> GetDependencies<TObject>(TObject item, DependencyFlags flags);
    }
}