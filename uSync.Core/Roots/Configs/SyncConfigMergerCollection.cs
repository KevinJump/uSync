using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

namespace uSync.Core.Roots.Configs;
public class SyncConfigMergerCollection
    : BuilderCollectionBase<ISyncConfigMerger>
{
    public SyncConfigMergerCollection(Func<IEnumerable<ISyncConfigMerger>> items) 
        : base(items)
    { }

    public ISyncConfigMerger GetConfigMerger(string editorAlias)
        => this.FirstOrDefault(x => x.Editors.InvariantContains(editorAlias));
}


public class SyncConfigMergerCollectionBuilder
    : WeightedCollectionBuilderBase<SyncConfigMergerCollectionBuilder,
        SyncConfigMergerCollection, ISyncConfigMerger>
{
    protected override SyncConfigMergerCollectionBuilder This => this;
}