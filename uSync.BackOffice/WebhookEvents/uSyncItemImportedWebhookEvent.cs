using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  webhook event for when a single item has been imported
/// </summary>
[WebhookEvent("uSync item imported")]
public class uSyncItemImportedWebhookEvent : 
    WebhookEventBase<uSyncImportedItemNotification>
{
    /// <inheritdoc/>
    public uSyncItemImportedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webhookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor) 
        : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    /// <inheritdoc/>
    public override string Alias => "uSyncItemImported";
}

