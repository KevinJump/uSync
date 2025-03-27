using Microsoft.AspNetCore.Http;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Umbraco.Extensions;

using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core.Models;

namespace uSync.BackOffice;

/// <summary>
/// Extensions for working with uSyncActions
/// </summary>
public static class uSyncActionExtensions
{
    /// <summary>
    ///  does this list of actions have any that in an error state?
    /// </summary>
    public static bool ContainsErrors(this IEnumerable<uSyncAction> actions)
        => actions.Any(x => x.Change != Core.ChangeType.Hidden && x.Change >= Core.ChangeType.Fail || !x.Success);

    /// <summary>
    ///  does this list of actions have any that in an error state?
    /// </summary>
    public static int CountErrors(this IEnumerable<uSyncAction> actions)
        => actions.Count(x => x.Change != Core.ChangeType.Hidden && x.Change >= Core.ChangeType.Fail || !x.Success);

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
            !actions.Any() ||
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
            if (items.Key is null) continue;

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
        action = actions.FirstOrDefault(x => $"{x.Key}_{x.HandlerAlias}" == $"{key}_{handlerAlias}", new uSyncAction { Key = Guid.Empty });
        return action.Key != Guid.Empty;
    }

    /// <summary>
    /// merge two lists of actions together, removing duplicates
    /// </summary>
    public static List<uSyncAction> Merge(this List<uSyncAction> a, List<uSyncAction> b)
    {
        // quicker than doing all that link.
        if (b.Count == 0) return a;
        if (a.Count == 0) return b;

        // everythin that is in a but not b.
        var actions = a.Where(x => b.Any(y => x.Key == y.Key && x.HandlerAlias == y.HandlerAlias) is false).ToList();
        return [.. actions, .. b];
    }

    /// <summary>
    ///  update any existing action with the details from the attempt. 
    /// </summary>
    public static void UpdateActions<TObject>(this List<uSyncAction> actions, Guid key, string handlerAlias, SyncAttempt<TObject> attempt)
    {
        if (key == Guid.Empty) return;

        // if it's not an error, and has a no message and blank details, its not worth updating, 
        // so we skit the lookup and update (worth it as the list may have 10000's of items)
        if (attempt.Success == true 
            && string.IsNullOrWhiteSpace(attempt.Message) is true 
            && attempt.Details?.Count() == 0) return;

        if (actions.TryFindAction(key, handlerAlias, out var action))
        {
            actions.Remove(action);
            action.Message += attempt.Message;
            action.Details = [.. action.Details ?? [], .. attempt.Details ?? []];

            action.Success = attempt.Success;

            if (attempt.Success is false)
            {
                action.Message = "Failed: " + action.Message;
                action.Exception = attempt.Exception;
                action.Change = Core.ChangeType.Fail;
            }
            actions.Add(action);
        }
    }

    /// <summary>
    ///  does the item in this attempt need to be saved ?
    /// </summary>
    /// <remarks>
    ///  if something comes back successful, and it wasn't saved by the serializer
    ///  then we might want to save it ourselves. 
    /// </remarks>
    public static bool RequiresSave<TObject>(this SyncAttempt<TObject> attempt)
        => attempt.Success && attempt.Change > Core.ChangeType.NoChange && !attempt.Saved && attempt.Item != null;
}
