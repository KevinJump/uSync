using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Core.Webhooks;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  webhook event for when an import has been completed
/// </summary>
public class uSyncExportCompletedWebhookEvent :
    WebhookEventBase<uSyncExportCompletedNotification>
{
    /// <inheritdoc/>
    public uSyncExportCompletedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webhookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor) : base(webhookFiringService, webhookService, webhookSettings, serverRoleAccessor)
    { }

    /// <inheritdoc/>
    public override string Alias => "uSync Export Completed";
}

