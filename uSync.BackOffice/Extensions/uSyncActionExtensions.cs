﻿using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Extensions;

using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice
{
    /// <summary>
    /// Extensions for working with uSyncActions
    /// </summary>
    public static class uSyncActionExtensions
    {
        /// <summary>
        ///  does this list of actions have any that in an error state?
        /// </summary>
        public static bool ContainsErrors(this IEnumerable<uSyncAction> actions)
            => actions.Any(x => x.Change >= Core.ChangeType.Fail || !x.Success);

        /// <summary>
        ///  count how many actions in this list are for changes
        /// </summary>
        public static int CountChanges(this IEnumerable<uSyncAction> actions)
            => actions.Count(x => x.Change > Core.ChangeType.NoChange && x.Change < Core.ChangeType.Hidden);

        /// <summary>
        ///  checks to see if the requested action is valid for the configured list of actions.
        /// </summary>
        public static bool IsValidAction(this HandlerActions requestedAction, IEnumerable<string> actions)
            => requestedAction == HandlerActions.None ||
                actions.Count() == 0 ||
                actions.InvariantContains("all") ||
                actions.InvariantContains(requestedAction.ToString());

        /// <summary>
        ///  Convert a list of actions into a summary list of actions, uses less cpu when people sync massive amounts of content.
        /// </summary>
        public static IEnumerable<uSyncAction> ConvertToSummary(this IEnumerable<uSyncAction> actions, bool strict)
        {
            var summary = new List<uSyncAction>();

            foreach (var items in actions.GroupBy(x => x.HandlerAlias))
            {
                var fails = items.Where(x => !x.Success).ToList();

                summary.Add(uSyncAction.SetAction(
                    success: true,
                    name: items.Key,
                    type: items.Key,
                    change: Core.ChangeType.Information,
                    message: $"({items.CountChanges()}/{items.Count()} Changes) ({fails.Count} failures)")
                );

                if (!strict) summary.AddRange(fails);

            }

            return summary;
        }

        /// <summary>
        ///  try to find an action in the list based on key, and handler alias
        /// </summary>
        public static bool TryFindAction(this IEnumerable<uSyncAction> actions, Guid key, string handlerAlias, out uSyncAction action)
        {
            action = actions.FirstOrDefault(x => $"{x.key}_{x.HandlerAlias}" == $"{key}_{handlerAlias}", new uSyncAction { key = Guid.Empty });
            return action.key != Guid.Empty;
        }

    }
}
