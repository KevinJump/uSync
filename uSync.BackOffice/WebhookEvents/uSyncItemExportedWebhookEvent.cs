using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  webhook event for when an single item has been exported
/// </summary>
[WebhookEvent("uSync item exported")]
public class uSyncItemExportedWebhookEvent :
    WebhookEventBase<uSyncExportedItemNotification>
{
    /// <inheritdoc/>
    public uSyncItemExportedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webhookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor) : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    /// <inheritdoc/>
    public override string Alias => "uSyncItemExported";
}
