using System;
using System.Collections.Generic;

namespace uSync.BackOffice.Configuration
{
    public class uSyncHandlerSetSettings
    {
        /// <summary>
        ///  is this handler set enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        ///  list of groups handlers can belong to.
        /// </summary>
        public string[] HandlerGroups { get; set; } = Array.Empty<string>();

        /// <summary>
        ///  list of disabled handlers
        /// </summary>
        public string[] DisabledHandlers { get; set; } = Array.Empty<string>();
        
        /// <summary>
        ///  default settings for all handlers
        /// </summary>
        public HandlerSettings HandlerDefaults { get; set; } = new HandlerSettings();

        /// <summary>
        ///  settings for named handlers 
        /// </summary>
        public IDictionary<string, HandlerSettings> Handlers { get; set; } = new Dictionary<string, HandlerSettings>();

    }

    public static class HandlerSetSettingsExtensions
    {
        /// <summary>
        ///  Get the handler settings for the named handler - (will load defaults if no speicifc handler settings are found)
        /// </summary>
        public static HandlerSettings GetHandlerSettings(this uSyncHandlerSetSettings handlerSet, string alias)
        {
            if (handlerSet.Handlers.ContainsKey(alias))
                return handlerSet.Handlers[alias].Clone();

            return handlerSet.HandlerDefaults.Clone();
        }
    }

}
