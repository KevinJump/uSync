using System.Text.Json.Serialization;

namespace uSync.Core.Dependency;

/// <summary>
///  Flags to control how item dependencies are calculated.
/// </summary>
[Flags()]
[JsonConverter(typeof(JsonStringEnumConverter<DependencyFlags>))]
public enum DependencyFlags
{
    /// <summary>
    ///  no flags
    /// </summary>
    None = 0,

    /// <summary>
    ///  include child items 
    /// </summary>
    IncludeChildren = 2,

    /// <summary>
    ///  include the parent items
    /// </summary>
    IncludeAncestors = 4,

    /// <summary>
    ///  include system dependencies (doctypes, datatypes, etc)
    /// </summary>
    IncludeDependencies = 8,

    /// <summary>
    ///  include the view files (template files)
    /// </summary>
    IncludeViews = 16,

    /// <summary>
    ///  include any media linked to an item
    /// </summary>
    IncludeMedia = 32,

    /// <summary>
    ///  include any linked content
    /// </summary>
    IncludeLinked = 64,

    /// <summary>
    ///  include the physical media files 
    /// </summary>
    IncludeMediaFiles = 128,

    /// <summary>
    ///  include configuration elements such as public access or domain settings
    /// </summary>
    IncludeConfig = 256,

    /// <summary>
    ///  only include directly adjacent items (don't recurse)
    /// </summary>
    AdjacentOnly = 512,

    /// <summary>
    ///  calculate as part of a root tree (alters how ancestors are calculated)
    /// </summary>
    RootSync = 1024,

    /// <summary>
    ///  include the contents of any template, or view as part of the sync.
    /// </summary>
    IncludeContents = 2048,

    /// <summary>
    ///  dependencies should be checked against published values.
    /// </summary>
    PublishedDependencies = 4096, // dependencies should be checked against published values.

    /// <summary>
    ///  include items that are in the recycle bin.
    /// </summary>
    /// <remarks>
    ///  set for the item being synced, not for what it depends on, so a binned item that
    ///  something live links to isn't picked up along the way. Checkers decide what to
    ///  do with a binned item when it is set.
    /// </remarks>
    IncludeTrashed = 8192
}
