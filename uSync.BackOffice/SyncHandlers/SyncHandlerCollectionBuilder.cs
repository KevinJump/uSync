using System.Collections.Generic;

using Umbraco.Cms.Core.Composing;

namespace uSync.BackOffice.SyncHandlers
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
        { }

        public IEnumerable<ISyncHandler> Handlers => this;


    }
}
