using System.Collections.Generic;
using System.Xml.Linq;

using uSync.Core.Cache;
using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Core
{
    public class SyncItemFactory : ISyncItemFactory
    {
        private readonly SyncTrackerCollection syncTrackers;
        private readonly SyncDependencyCollection syncCheckers;

        private readonly SyncEntityCache entityCache;

        private readonly SyncSerializerCollection syncSerializers;


        public SyncItemFactory(
            SyncEntityCache entityCache,
            SyncSerializerCollection syncSerializers,
            SyncTrackerCollection syncTrackers,
            SyncDependencyCollection syncCheckers)
        {
            this.syncSerializers = syncSerializers;
            this.syncTrackers = syncTrackers;
            this.syncCheckers = syncCheckers;
            this.entityCache = entityCache;
        }

        public SyncEntityCache EntityCache => entityCache;

        public IEnumerable<ISyncSerializer<TObject>> GetSerializers<TObject>()
            => syncSerializers.GetSerializers<TObject>();

        public ISyncSerializer<TObject> GetSerializer<TObject>(string name)
            => syncSerializers.GetSerializer<TObject>(name);


        public IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>()
            => syncTrackers.GetTrackers<TObject>();

        public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, SyncSerializerOptions options)
            => syncTrackers.GetChanges<TObject>(node, options);
        public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options)
        {
            if (currentNode == null)
                return syncTrackers.GetChanges<TObject>(node, options);
            else
                return syncTrackers.GetChanges<TObject>(node, currentNode, options);
        }


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
