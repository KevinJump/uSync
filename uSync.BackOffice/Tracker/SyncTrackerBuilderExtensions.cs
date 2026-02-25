using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.DependencyInjection;

namespace uSync.BackOffice.Tracker;

internal static class SyncTrackerBuilderExtensions
{
    public static IUmbracoBuilder AddSyncTracker(this IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<ISyncTrackerService, SyncTrackerService>();
        builder.AddNotificationAsyncHandler<uSyncImportCompletedNotification, SyncTrackerNotificationHandler>();
        return builder;
    }
}
