using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Composing;

namespace uSync8.BackOffice.SyncHandlers
{
    public class SyncHandlerCollectionBuilder
        : LazyCollectionBuilderBase<SyncHandlerCollectionBuilder, SyncHandlerCollection, ISyncHandler> 
    {
        protected override SyncHandlerCollectionBuilder This => this;
    }

    public class SyncHandlerCollection : BuilderCollectionBase<ISyncHandler>
    {
        public SyncHandlerCollection(IEnumerable<ISyncHandler> items)
            : base(items)
        {
        }
    }
}
