using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;

namespace uSync8.Core.Dependency
{
    /*
    public class SyncDependencyCollectionBuilder
        : LazyCollectionBuilderBase<SyncDependencyCollectionBuilder, SyncDependencyCollection, ISyncDependencyItem>
    {
        protected override SyncDependencyCollectionBuilder This => this;
    }

    public class SyncDependencyCollection : BuilderCollectionBase<ISyncDependencyItem>
    {
        public SyncDependencyCollection(IEnumerable<ISyncDependencyItem> items) 
            : base(items)
        {
        }

        public ISyncDependencyChecker<TObject> GetChecker<TObject>(UmbracoObjectTypes objectType)
            where TObject : IEntity
        {
            var checker = this.FirstOrDefault(x => x.ObjectType == objectType);
            if (checker is ISyncDependencyChecker<TObject> objectChecker)
            {
                return objectChecker;
            }

            return null;
        }
    }
    */
}
