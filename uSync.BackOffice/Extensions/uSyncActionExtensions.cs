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
            => actions.Count(x => x.Change > Core.ChangeType.NoChange);

        /// <summary>
        ///  checks to see if the reuqested action is valid for the configured list of actions.
        /// </summary>
        public static bool IsValidAction(this HandlerActions requestedAction, IEnumerable<string> actions)
            => requestedAction == HandlerActions.None ||
                actions.Count() == 0 || 
                actions.InvariantContains("all") ||
                actions.InvariantContains(requestedAction.ToString());

    }
}
