using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using NPoco.Expressions;

using uSync8.Core.Dependency;
using uSync8.Core.Models;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.Core
{
    public class SyncItemFactory : ISyncItemFactory
    {
        private readonly SyncTrackerCollection syncTrackers;
        private readonly SyncDependencyCollection syncCheckers;

        public SyncItemFactory(
            SyncTrackerCollection syncTrackers,
            SyncDependencyCollection syncCheckers)
        {
            this.syncTrackers = syncTrackers;
            this.syncCheckers = syncCheckers;
        }

        public IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>()
            => syncTrackers.GetTrackers<TObject>();

        public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, SyncSerializerOptions options)
            => syncTrackers.GetChanges<TObject>(node, options);
   
        public IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>()
            => syncCheckers.GetCheckers<TObject>();

        public IEnumerable<uSyncDependency> GetDependencies<TObject>(TObject item, DependencyFlags flags)
        {
            var dependencies = new List<uSyncDependency>();
            foreach (var checker in syncCheckers.GetCheckers<TObject>())
            {
                dependencies.AddRange(checker.GetDependencies(item, flags));
            }
            return dependencies;
        }
    }
}
