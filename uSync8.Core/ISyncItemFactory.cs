using System.Collections.Generic;
using System.Xml.Linq;

using uSync8.Core.Dependency;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.Core
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