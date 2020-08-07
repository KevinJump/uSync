using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.BackOffice
{
    public static class uSyncActionExtensions
    {
        /// <summary>
        ///  does this list of actions have any that in an error state?
        /// </summary>
        public static bool ContainsErrors(this IEnumerable<uSyncAction> actions)
            => actions.Any(x => x.Change >= Core.ChangeType.Fail);

        /// <summary>
        ///  count how many actions in this list are for changes
        /// </summary>
        public static int CountChanges(this IEnumerable<uSyncAction> actions)
            => actions.Count(x => x.Change > Core.ChangeType.NoChange);
    }
}
