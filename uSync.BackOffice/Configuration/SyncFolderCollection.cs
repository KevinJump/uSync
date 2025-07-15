using J2N.Collections.Generic.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Composing;

namespace uSync.BackOffice.Configuration;

/// <summary>
///  collection of folders loaded from extensions, merged into the 
///  folders from settings at runtime. 
/// </summary>
public class SyncFolderCollection : BuilderCollectionBase<ISyncFolder>
{
    public SyncFolderCollection(Func<IEnumerable<ISyncFolder>> items)
        : base(items)
    { }

    /// <summary>
    ///  returns an array of all the folders in the collection.
    /// </summary>
    /// <returns></returns>
    public ISyncFolder[] GetFolders()
        => this.ToArray();
}

/// <summary>
///  collection builder to mange folders. 
/// </summary>
public class SyncFolderCollectionBuilder
    : SetCollectionBuilderBase<SyncFolderCollectionBuilder, SyncFolderCollection, ISyncFolder>
{
    protected override SyncFolderCollectionBuilder This => this;
}

