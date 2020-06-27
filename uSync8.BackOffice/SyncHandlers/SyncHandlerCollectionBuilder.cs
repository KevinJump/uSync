using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;

using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.SyncHandlers
{
    public class SyncHandlerCollectionBuilder
        : LazyCollectionBuilderBase<SyncHandlerCollectionBuilder, SyncHandlerCollection, ISyncHandler>
    {
        protected override SyncHandlerCollectionBuilder This => this;
    }

    public class SyncHandlerCollection : BuilderCollectionBase<ISyncHandler>
    {
        /// <summary>
        ///  handlers that impliment the Extended Handler interface, can be used for other things.
        /// </summary>
        private List<ISyncExtendedHandler> extendedHandlers;

        public SyncHandlerCollection(IEnumerable<ISyncHandler> items)
            : base(items)
        {
            extendedHandlers = items
                .Where(x => x is ISyncExtendedHandler)
                .Select(x => x as ISyncExtendedHandler)
                .ToList();
        }

        public IEnumerable<ISyncHandler> Handlers => this;

        public IEnumerable<ISyncExtendedHandler> ExtendedHandlers
            => extendedHandlers;
    }
}
