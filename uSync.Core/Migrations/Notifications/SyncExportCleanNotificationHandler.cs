using Umbraco.Cms.Core.Events;

namespace uSync.Core.Migrations.Notifications;

internal class SyncExportCleanNotificationHandler : INotificationAsyncHandler<SyncExportCleanNotification>
{
    private readonly ISyncMigratedDataService _syncMigratedDataService;

    public SyncExportCleanNotificationHandler(ISyncMigratedDataService syncMigratedDataService)
    {
        _syncMigratedDataService = syncMigratedDataService;
    }

    public async Task HandleAsync(SyncExportCleanNotification notification, CancellationToken cancellationToken)
    {
        await _syncMigratedDataService.DeleteAllAsync();
    }
}
