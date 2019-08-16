namespace uSync8.BackOffice.SyncHandlers
{
    /// <summary>
    ///  options that define how we define a handler 
    /// </summary>
    public class SyncHandlerOptions
    {
        /// <summary>
        ///  The Handler set, defined in the uSync.Config file
        /// </summary>
        public string Set { get; set; } = string.Empty;

        /// <summary>
        ///  Handler grouping (settings, content, etc) 
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        ///  What action do you want to perform.
        /// </summary>
        public HandlerActions Action { get; set; } = HandlerActions.None;


        public SyncHandlerOptions() { }

        public SyncHandlerOptions(string setName)
            : this()
        {
            this.Set = setName;
        }
    }


}
