namespace uSync.BackOffice.Models
{
    /// <summary>
    ///  view model of a handler, sent to the UI to draw the handler boxes.
    /// </summary>
    public class SyncHandlerView
    {
        /// <summary>
        ///  Is Handler enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///  current status of this handler 
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Display name of the handler 
        /// </summary>
        public required string Name { get; set; }
        
        /// <summary>
        /// Alias for the handler 
        /// </summary>
        public required string Alias { get; set; }

        /// <summary>
        /// Icon to show for handler 
        /// </summary>
        public string Icon { get; set; } = "icon-bug";

        /// <summary>
        /// Group handler belongs too
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Set handler is currently in
        /// </summary>
        public string Set { get; set; } = string.Empty;
    }
}
