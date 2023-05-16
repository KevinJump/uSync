using System;

namespace uSync.Core.Dependency
{
    [Flags]
    public enum DependencyFlags
    {
        None = 0,
        IncludeChildren = 2, // include children of this item
        IncludeAncestors = 4, // include parents of this item
        IncludeDependencies = 8, // include system dependencies (doctypes, datatypes, etc)
        IncludeViews = 16, // include the view files required for the item (template)
        IncludeMedia = 32, // include any media linked to by this item
        IncludeLinked = 64, // include any content linked to by this item
        IncludeMediaFiles = 128, // include the physical media items 
        IncludeConfig = 256, // include config elements such as public access, or domain settings
        AdjacentOnly = 512, // only include direclty adjectent items (don't go right down or up the tree)
        RootSync = 1024, // calculate as part of a sync from the root of a tree. (changes how ancestor syncs are handled)
        IncludeContents = 2048, // include the contents of any file we might be syncing (e.g templates).
        PublishedDependencies = 4096 // dependencies should be checked against published values.
    }
}
