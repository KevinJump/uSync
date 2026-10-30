using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Composing;

using uSync.BackOffice.Models;

namespace uSync.BackOffice.Preview;

/// <summary>
///  the preview mappers, in weight order.
/// </summary>
public class SyncPreviewMapperCollection : BuilderCollectionBase<ISyncPreviewMapper>
{
    /// <inheritdoc/>
    public SyncPreviewMapperCollection(Func<IEnumerable<ISyncPreviewMapper>> items)
        : base(items)
    { }

    /// <summary>
    ///  the first mapper that can map the preview.
    /// </summary>
    /// <remarks>
    ///  give a mapper a lower <see cref="WeightAttribute"/> to put it in front of the uSync ones.
    /// </remarks>
    public ISyncPreviewMapper? GetMapper(SyncItemPreview preview)
        => this.FirstOrDefault(x => x.CanMap(preview));
}

/// <summary>
///  collection builder for the preview mappers.
/// </summary>
public class SyncPreviewMapperCollectionBuilder
    : WeightedCollectionBuilderBase<SyncPreviewMapperCollectionBuilder, SyncPreviewMapperCollection, ISyncPreviewMapper>
{
    /// <inheritdoc/>
    protected override SyncPreviewMapperCollectionBuilder This => this;
}
