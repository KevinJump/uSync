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
        IncludeFiles = 16,
        IncludeMedia = 32,
        IncludeLinked = 64

    }
}
