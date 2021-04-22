using System;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  Attribute used to markup a handler in code.
    /// </summary>
    public class SyncHandlerAttribute : Attribute
    {
        public SyncHandlerAttribute(string alias, string name, string folder, int priority)
        {
            Alias = alias;
            Name = name;
            Priority = priority;
            Folder = folder;
        }

        /// <summary>
        ///  Name of the handler
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  Alias used when finding the handler 
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        ///  order of execution (lower is first)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        ///  default folder name for items to be stored in
        /// </summary>
        public string Folder { get; set; }

        /// <summary>
        ///  does the handler require two passes at an item to import it.
        /// </summary>
        public bool IsTwoPass { get; set; } = false;

        /// <summary>
        ///  icon for handler used in UI
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        ///  Umbraco Entity type handler works with
        /// </summary>
        public string EntityType { get; set; }
    }
}
