using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Umbraco.Core.Composing;

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
            foreach(var tracker in GetTrackers<TObject>())
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
    }
#pragma warning restore CS0618 // Type or member is obsolete

    public class SyncTrackerCollectionBuilder
        : WeightedCollectionBuilderBase<SyncTrackerCollectionBuilder,
            SyncTrackerCollection, ISyncTrackerBase>
    {
        protected override SyncTrackerCollectionBuilder This => this;
    }
}
