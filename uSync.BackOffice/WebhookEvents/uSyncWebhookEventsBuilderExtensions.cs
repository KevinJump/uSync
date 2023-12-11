using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  extension class so people can add uSync webhooks, via fluent API
/// </summary>
public static class uSyncWebhookEventsBuilderExtensions
{
    /// <summary>
    ///  Add the uSync webhooks, so you can create webhooks for uSync events
    /// </summary>
    public static IUmbracoBuilder AdduSyncWebhooks(this IUmbracoBuilder builder)
    {
        builder.WebhookEvents()
            .Add<uSyncImportCompletedWebhookEvent>()
            .Add<uSyncExportCompletedWebhookEvent>()
            .Add<uSyncItemImportedWebhookEvent>()
            .Add<uSyncItemExportedWebhookEvent>();

        return builder;
    }
}
