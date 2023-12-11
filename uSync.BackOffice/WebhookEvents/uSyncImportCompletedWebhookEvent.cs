using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;


namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  Webhook Event for when an import process has been completed
/// </summary>
[WebhookEvent("uSync import completed")]
public class uSyncImportCompletedWebhookEvent :
    WebhookEventBase<uSyncImportCompletedNotification>
{
    /// <inheritdoc/>
    public uSyncImportCompletedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webhookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor) : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    { }

    /// <inheritdoc/>
    public override string Alias => "uSyncImportCompleted";
}

