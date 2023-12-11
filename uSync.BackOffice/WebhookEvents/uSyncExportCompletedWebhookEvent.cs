using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;

namespace uSync.BackOffice.WebhookEvents;

public class uSyncExportCompletedWebhookEvent :
    WebhookEventBase<uSyncExportCompletedNotification>
{
    public uSyncExportCompletedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webhookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor) : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    { }

    public override string Alias => "uSync Export Completed";
}

