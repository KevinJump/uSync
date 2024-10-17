using System;
using System.Collections.Generic;

using Umbraco.Cms.Core.Composing;

using uSync.BackOffice.SyncHandlers.Interfaces;

namespace uSync.BackOffice.SyncHandlers;

/// <summary>
/// Collection builder for SyncHandlers
/// </summary>
public class SyncHandlerCollectionBuilder
    : LazyCollectionBuilderBase<SyncHandlerCollectionBuilder, SyncHandlerCollection, ISyncHandler>
{
    /// <inheritdoc/>
    protected override SyncHandlerCollectionBuilder This => this;
}

/// <summary>
/// A collection of SyncHandlers
/// </summary>
public class SyncHandlerCollection : BuilderCollectionBase<ISyncHandler>
{
    /// <summary>
    ///  Construct a collection of handlers from a list of handler items 
    /// </summary>
    public SyncHandlerCollection(Func<IEnumerable<ISyncHandler>> items)
        : base(items)
    { }

    /// <summary>
    ///  Handlers in the collection
    /// </summary>
    public IEnumerable<ISyncHandler> Handlers => this;


}
