using System;
using System.Collections.Generic;
using System.Linq;

using uSync.Core.Models;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  Payload sent by the import/export completed webhook events
/// </summary>
public class uSyncBulkWebhookPayload
{
    /// <summary>
    ///  the most failed items listed in <see cref="Failures"/>
    /// </summary>
    public const int MaxFailures = 50;

    /// <summary>
    ///  name of the server that ran the operation, so one receiver can tell sites apart
    /// </summary>
    public string? Server { get; set; }

    /// <summary>
    ///  The group e.g settings, content that this action ran under.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    ///  summary counts for the operation
    /// </summary>
    public uSyncBulkWebhookSummary Summary { get; set; } = new();

    /// <summary>
    ///  the failed actions (at most <see cref="MaxFailures"/>, see <see cref="Summary"/> for the full count)
    /// </summary>
    public List<uSyncActionWebhookPayload> Failures { get; set; } = [];

    /// <summary>
    ///  actions that have occured during the bulk operation
    /// </summary>
    public List<uSyncActionWebhookPayload> Actions { get; set; } = [];

    /// <summary>
    ///  build a payload from a bulk notification
    /// </summary>
    public static uSyncBulkWebhookPayload FromNotification(uSyncBulkNotification notification)
    {
        var actions = notification.Actions?.ToList() ?? [];

        return new()
        {
            Server = Environment.MachineName,
            Group = notification.Group,
            Summary = uSyncBulkWebhookSummary.FromActions(actions),
            Failures = actions
                .Where(IsFailure)
                .Take(MaxFailures)
                .Select(uSyncActionWebhookPayload.FromAction)
                .ToList(),
            Actions = actions
                .Select(uSyncActionWebhookPayload.FromAction)
                .ToList()
        };
    }

    // same rule as the error counts, so Failures and Summary.Failed agree.
    private static bool IsFailure(uSyncAction action)
        => new[] { action }.ContainsErrors();
}
