using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Dependency
{
    public class SyncDependencyCollection
        : BuilderCollectionBase<ISyncDependencyItem>
    {
        public SyncDependencyCollection(IEnumerable<ISyncDependencyItem> items)
            : base(items) { }

        public IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>()
        {
            return this.Where(x => x is ISyncDependencyChecker<TObject> checker)
                .Select(x => x as ISyncDependencyChecker<TObject>);
        }
    }

    public class SyncDependencyCollectionBuilder
        : WeightedCollectionBuilderBase<SyncDependencyCollectionBuilder, SyncDependencyCollection, ISyncDependencyItem>
    {

        protected override SyncDependencyCollectionBuilder This => this;
    }
}
