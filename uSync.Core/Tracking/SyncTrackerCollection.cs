using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Cms.Core.Composing;

using uSync.Core.Models;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking
{
    public class SyncTrackerCollection
           : BuilderCollectionBase<ISyncTrackerBase>
    {
        public SyncTrackerCollection(IEnumerable<ISyncTrackerBase> items)
            : base(items)
        { }

        public IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>()
        {
            return this.Where(x => x is ISyncTracker<TObject> tracker)
                .Select(x => x as ISyncTracker<TObject>);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, SyncSerializerOptions options)
        {
            var changes = new List<uSyncChange>();
            foreach (var tracker in GetTrackers<TObject>())
            {
                if (tracker is ISyncOptionsTracker<TObject> optionTracker)
                    changes.AddRange(optionTracker.GetChanges(node, options));
                else
                {
                    changes.AddRange(tracker.GetChanges(node));
                }
            }
            return changes;
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public IEnumerable<uSyncChange> GetChanges<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options)
        {
            if (currentNode == null)
                return GetChanges<TObject>(node, options);

            var changes = new List<uSyncChange>();
            foreach (var tracker in GetTrackers<TObject>())
            {
                switch (tracker)
                {
                    case ISyncNodeTracker<TObject> nodeTracker:
                        changes.AddRange(nodeTracker.GetChanges(node, currentNode, options));
                        break;
                    case ISyncOptionsTracker<TObject> optionTracker:
                        changes.AddRange(optionTracker.GetChanges(node, options));
                        break;
                    default:
#pragma warning disable CS0618 // Type or member is obsolete
                        changes.AddRange(tracker.GetChanges(node));
#pragma warning restore CS0618 // Type or member is obsolete
                        break;
                }
            }
            return changes;
        }

    }



    public class SyncTrackerCollectionBuilder
        : WeightedCollectionBuilderBase<SyncTrackerCollectionBuilder,
            SyncTrackerCollection, ISyncTrackerBase>
    {
        protected override SyncTrackerCollectionBuilder This => this;
    }
}
