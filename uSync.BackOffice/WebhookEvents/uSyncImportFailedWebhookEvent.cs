using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  Webhook event for when an import has completed and one or more items failed.
/// </summary>
/// <remarks>
///  Fires from the same notification as <see cref="uSyncImportCompletedWebhookEvent"/>,
///  but only when the import contains errors, so a receiver can alert on failures only.
/// </remarks>
[WebhookEvent("uSync import failed")]
public class uSyncImportFailedWebhookEvent :
    WebhookEventBase<uSyncImportCompletedNotification>
{
    /// <inheritdoc/>
    public uSyncImportFailedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webhookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor) : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    { }

    /// <inheritdoc/>
    public override string Alias => "uSyncImportFailed";

    /// <inheritdoc/>
    public override bool ShouldFireWebhookForNotification(uSyncImportCompletedNotification notificationObject)
        => notificationObject.Actions?.ContainsErrors() is true;

    /// <inheritdoc/>
    public override object? ConvertNotificationToRequestPayload(uSyncImportCompletedNotification notification)
        => uSyncBulkWebhookPayload.FromNotification(notification);
}
