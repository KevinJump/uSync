using System;

namespace uSync8.Core.Dependency
{
    [Flags]
    public enum DependencyFlags
    {
        None = 0,
        IncludeChildren = 2,
        IncludeAncestors = 4,
        IncludeDependencies = 8,
        IncludeViews = 16,
        IncludeMedia = 32,
        IncludeLinked = 64,
        IncludeMediaFiles = 128,
        IncludeConfig = 256
    }
}
