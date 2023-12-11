using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.BackOffice.WebhookEvents;
public static class uSyncWebhookEventsBuilderExtensions
{
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
