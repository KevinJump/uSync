﻿namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    ///  options that define how we define a handler 
    /// </summary>
    public class SyncHandlerOptions
    {
        /// <summary>
        ///  Handler grouping (settings, content, etc) 
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        ///  What action do you want to perform.
        /// </summary>
        public HandlerActions Action { get; set; } = HandlerActions.None;

        /// <summary>
        ///  handler set
        /// </summary>
        public string Set { get; set; } = uSync.Sets.DefaultSet;

        /// <summary>
        ///  include handlers that are by default disabled 
        /// </summary>
        public bool IncludeDisabled { get; set; } = false;

        /// <summary>
        ///  the user id doing all the work.
        /// </summary>
        public int UserId { get; set; } = -1;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SyncHandlerOptions() { }

        /// <summary>
        /// Construct Options for a given set
        /// </summary>
        public SyncHandlerOptions(string setName)
            : this()
        {
            this.Set = setName;
        }

        /// <summary>
        ///  construct for a given site and userId
        /// </summary>
        public SyncHandlerOptions(string setName, int userId)
            :this(setName)
        {
            this.UserId = userId;
        }

        /// <summary>
        /// Construct options with set and handler action set.
        /// </summary>
        public SyncHandlerOptions(string setName, HandlerActions action)
            : this(setName)
        {
            this.Set = setName;
            this.Action = action;
        }

        /// <summary>
        ///  construct for a given set, action and userId
        /// </summary>
        public SyncHandlerOptions(string setName, HandlerActions action, int userId)
            : this(setName, action)
        {
            this.UserId = userId;
        }
    }


}
