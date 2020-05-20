using System;

namespace uSync8.Core.Dependency
{
    [Flags]
    public enum DependencyFlags
    {
        None = 0,

        /// <summary>
        ///  Include child items of this item
        /// </summary>
        IncludeChildren = 2,

        /// <summary>
        ///  Include parent items of this item
        /// </summary>
        IncludeAncestors = 4,

        /// <summary>
        ///  include any items required to create this item (e.g doctypes/datatypes)
        /// </summary>
        IncludeDependencies = 8,

        /// <summary>
        ///  include the view templates associated with an item
        /// </summary>
        IncludeViews = 16,

        /// <summary>
        ///  include any media used within a content item 
        /// </summary>
        IncludeMedia = 32,

        /// <summary>
        ///  include any items that are linked to (via pickers) of this item (can result in big dependency tree)
        /// </summary>
        IncludeLinked = 64,

        /// <summary>
        ///  include the actual media files (e.g the images) 
        /// </summary>
        IncludeMediaFiles = 128,

        /// <summary>
        ///  Include configuration related to an item (e.g domains/public access settings etc)
        /// </summary>
        IncludeConfig = 256
    }
}
