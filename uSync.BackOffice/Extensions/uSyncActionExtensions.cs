using System.Collections.Generic;
using System.Linq;

using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

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

        /// <summary>
        ///  Convert a list of actions into a summary list of actions, uses less cpu when people sync massive amounts of content.
        /// </summary>
       public static IEnumerable<uSyncAction> ConvertToSummary(this IEnumerable<uSyncAction> actions, bool strict)
        {
            var summary = new List<uSyncAction>();

            foreach(var items in actions.GroupBy(x => x.HandlerAlias))
            {
                var fails = items.Where(x => !x.Success).ToList();

                summary.Add(uSyncAction.SetAction(true, items.Key, items.Key, Core.ChangeType.Information,
                    $"({items.CountChanges()}/{items.Count()} Changes) ({fails.Count} failures)"));              

                if (!strict) summary.AddRange(fails);
               
            }

            return summary;
        }

    }
}
