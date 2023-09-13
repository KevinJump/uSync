using System;
using System.Collections.Generic;

using Umbraco.Cms.Core.Composing;

namespace uSync.BackOffice.Expansions;

/// <summary>
///  collection of UI tree nodes, allows us to dynamically extend the uSync tree
/// </summary>
public class SyncTreeNodeCollection
    : BuilderCollectionBase<ISyncTreeNode>
{
    /// <inheritdoc/>
    public SyncTreeNodeCollection(Func<IEnumerable<ISyncTreeNode>> items) 
        : base(items)
    { }
}

/// <summary>
///  collection builder for UI tree nodes under uSync tree.(subtrees)
/// </summary>
public class SyncTreeNodeCollectionBuilder 
    : LazyCollectionBuilderBase<SyncTreeNodeCollectionBuilder,
        SyncTreeNodeCollection, ISyncTreeNode>
{
    /// <inheritdoc/>
    protected override SyncTreeNodeCollectionBuilder This => this;
}
