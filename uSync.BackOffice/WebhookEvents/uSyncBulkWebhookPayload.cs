using System.Collections.Generic;
using System.Linq;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  Payload sent by the import/export completed webhook events
/// </summary>
public class uSyncBulkWebhookPayload
{
    /// <summary>
    ///  The group e.g settings, content that this action ran under.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    ///  actions that have occured during the bulk operation
    /// </summary>
    public List<uSyncActionWebhookPayload> Actions { get; set; } = [];

    /// <summary>
    ///  build a payload from a bulk notification
    /// </summary>
    public static uSyncBulkWebhookPayload FromNotification(uSyncBulkNotification notification)
        => new()
        {
            Group = notification.Group,
            Actions = notification.Actions?
                .Select(uSyncActionWebhookPayload.FromAction)
                .ToList() ?? []
        };
}
