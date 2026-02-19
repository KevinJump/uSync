using System;
using System.Collections.Generic;
using System.Text;

namespace uSync.Core.Roots.Models;

public enum SyncMergeStrategy
{
    /// <summary>
    ///  don't merge the value in the lowest folder wins.
    /// </summary>
    None,

    /// <summary>
    ///  merge at a 'block' level, new properties merge, new blocks or values at the top level
    /// </summary>
    Fancy,

    /// <summary>
    ///  merge down to the property level, so a property in a block will be merged individually.
    /// </summary>
    Magic,
}

public class SyncFileMergeOptions
{
    /// <summary>
    ///  what type of merging are we going to do.
    ///  <remarks>
    ///  This determines how the merging process will handle conflicts and overlapping content.
    ///  </remarks>
    public SyncMergeStrategy MergeStrategy { get; set; } = SyncMergeStrategy.Magic;
}