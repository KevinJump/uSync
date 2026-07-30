using Microsoft.AspNetCore.Http;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Umbraco.Extensions;

using uSync.BackOffice.Models;
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
            actions.InvariantContains(uSync.EverythingGroupName) ||
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
        action = actions.FirstOrDefault(x => x.Key == key && MatchesAlias(x.HandlerAlias, handlerAlias), new uSyncAction { Key = Guid.Empty });
        return action.Key != Guid.Empty;
    }

    /// <summary>
    ///  index the actions in a list by key and handler alias, so they can be found without scanning the list.
    /// </summary>
    /// <remarks>
    ///  the values are the positions of the actions in the list, so anything using the index has to
    ///  update the actions in place - if items are added, removed, or reordered the index is stale.
    /// </remarks>
    public static Dictionary<(Guid key, string handlerAlias), int> CreateActionIndex(this List<uSyncAction> actions)
    {
        var index = new Dictionary<(Guid, string), int>(actions.Count);
        for (var n = 0; n < actions.Count; n++)
        {
            // first one wins, so we find the same action as TryFindAction would.
            _ = index.TryAdd((actions[n].Key, actions[n].HandlerAlias ?? string.Empty), n);
        }
        return index;
    }

    private static bool MatchesAlias(string? actionAlias, string? handlerAlias)
        => (actionAlias ?? string.Empty) == (handlerAlias ?? string.Empty);

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
            actions.Add(ApplyAttempt(action, attempt));
        }
    }

    /// <summary>
    ///  update any existing action with the details from the attempt, using an index to find it.
    /// </summary>
    /// <remarks>
    ///  the action is updated in place, so the index built by <see cref="CreateActionIndex(List{uSyncAction})"/>
    ///  stays valid for the rest of the run.
    /// </remarks>
    public static void UpdateActions<TObject>(this List<uSyncAction> actions, Dictionary<(Guid key, string handlerAlias), int> index, Guid key, string handlerAlias, SyncAttempt<TObject> attempt)
    {
        if (key == Guid.Empty) return;

        // if it's not an error, and has a no message and blank details, its not worth updating,
        // so we skip the lookup and update (worth it as the list may have 10000's of items)
        if (attempt.Success == true
            && string.IsNullOrWhiteSpace(attempt.Message) is true
            && attempt.Details?.Count() == 0) return;

        if (index.TryGetValue((key, handlerAlias ?? string.Empty), out var position) is false) return;

        actions[position] = ApplyAttempt(actions[position], attempt);
    }

    private static uSyncAction ApplyAttempt<TObject>(uSyncAction action, SyncAttempt<TObject> attempt)
    {
        action.Message += attempt.Message;
        action.Details = [.. action.Details ?? [], .. attempt.Details ?? []];

        action.Success = attempt.Success;

        if (attempt.Success is false)
        {
            action.Message = "Failed: " + action.Message;
            action.Exception = attempt.Exception;
            action.Change = Core.ChangeType.Fail;
        }

        return action;
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

    /// <summary>
    ///  return the uSyncAction as an ActionView (used in the controllers)
    /// </summary>
    public static uSyncActionView AsActionView(this uSyncAction action)
    {
        var msg = string.IsNullOrWhiteSpace(action.Message) is false
            ? action.Message
            : string.IsNullOrWhiteSpace(action.Exception?.Message) is false
                ? action.Exception.Message
                : "";

        return new uSyncActionView
        {
            Key = action.Key,
            Name = action.Name,
            Handler = action.HandlerAlias ?? "",
            ItemType = action.ItemType,
            Change = action.Change,
            Success = action.Success,
            Details = action.Details?.ToList() ?? [],
            Message = msg
        };
    }
}
