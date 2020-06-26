using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core.Composing;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Tracking
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
    }

    public class SyncTrackerCollectionBuilder
        : WeightedCollectionBuilderBase<SyncTrackerCollectionBuilder,
            SyncTrackerCollection, ISyncTrackerBase>
    {
        protected override SyncTrackerCollectionBuilder This => this;
    }
}
