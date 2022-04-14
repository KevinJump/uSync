
using Microsoft.AspNetCore.DataProtection.KeyManagement;

using Umbraco.Extensions;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.SyncHandlers
{
    /// <summary>
    /// A Hanlder and its configuration
    /// </summary>
    public class HandlerConfigPair
    {
        /// <summary>
        /// Sync handler 
        /// </summary>
        public ISyncHandler Handler { get; set; }

        /// <summary>
        /// Loaded configuration for a handler 
        /// </summary>
        public HandlerSettings Settings { get; set; }
    }

    /// <summary>
    /// Extension methods for HandlerConfig pair class 
    /// </summary>
    public static class HandlerConfigPairExtensions
    {
        /// <summary>
        /// Is the handler enabled 
        /// </summary>
        public static bool IsEnabled(this HandlerConfigPair handlerAndConfig)
          => handlerAndConfig.Handler.Enabled && handlerAndConfig.Settings.Enabled;

        /// <summary>
        /// Is the handler valid for the given group?
        /// </summary>
        public static bool IsValidGroup(this HandlerConfigPair handlerAndConfig, string group)
        {
            // empty means all as does 'all'
            if (string.IsNullOrWhiteSpace(group) || group.InvariantEquals("all")) return true;

            var handlerGroup = handlerAndConfig.Handler.Group;
            if (!string.IsNullOrWhiteSpace(handlerAndConfig.Settings.Group))
            {
                handlerGroup = handlerAndConfig.Settings.Group;
            }

            return group.InvariantContains(handlerGroup);
        }

        /// <summary>
        /// Is the handler valid for the given action
        /// </summary>
        public static bool IsValidAction(this HandlerConfigPair handlerAndConfig, HandlerActions action)
        {
            if (action.IsValidAction(handlerAndConfig.Settings.Actions)) return true;
            return false;
        }

        /// <summary>
        /// What group is the handler configured to be in
        /// </summary>
        /// <remarks> 
        /// Gets the group from config or returns the default group for the selected handler
        /// </remarks>
        public static string GetConfigGroup(this HandlerConfigPair handlerConfigPair)
        {
            if (!string.IsNullOrWhiteSpace(handlerConfigPair.Settings.Group))
                return handlerConfigPair.Settings.Group;

            return handlerConfigPair.Handler.Group;
        }

        /// <summary>
        /// Get the icon for the group the handler belongs to
        /// </summary>
        public static string GetGroupIcon(this HandlerConfigPair handlerConfigPair)
        {
            var group = GetConfigGroup(handlerConfigPair);

            if (uSyncConstants.Groups.Icons.ContainsKey(group))
                return uSyncConstants.Groups.Icons[group];

            return handlerConfigPair.Handler.Icon;
        }
    }
}
