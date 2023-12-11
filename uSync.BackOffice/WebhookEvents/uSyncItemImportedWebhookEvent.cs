using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;

namespace uSync.BackOffice.WebhookEvents;

[WebhookEvent("uSync item imported")]
public class uSyncItemImportedWebhookEvent : 
    WebhookEventBase<uSyncImportedItemNotification>
{
    public uSyncItemImportedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webhookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor) 
        : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override string Alias => "uSyncItemImported";
}

