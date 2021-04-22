using System.Collections.Generic;

namespace uSync.BackOffice.Configuration
{
    public class uSyncHandlerSetSettings
    {
        public bool Enabled { get; set; } = true;

        public IEnumerable<string> DisabledHandlers { get; set; } = new List<string>();

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
        public static HandlerSettings GetHandlerSettings(this uSyncHandlerSetSettings handlerSet, string alias)
        {
            if (handlerSet.Handlers.ContainsKey(alias))
                return handlerSet.Handlers[alias].Clone();

            return handlerSet.HandlerDefaults.Clone();
        }
    }

}
