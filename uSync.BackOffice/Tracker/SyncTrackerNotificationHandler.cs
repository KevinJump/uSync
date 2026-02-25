using System.Threading;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Events;

namespace uSync.BackOffice.Tracker;

internal class SyncTrackerNotificationHandler
    : INotificationAsyncHandler<uSyncImportCompletedNotification>
{
    private readonly ISyncTrackerService _syncTrackerService;

    public SyncTrackerNotificationHandler(ISyncTrackerService syncTrackerService)
    {
        _syncTrackerService = syncTrackerService;
    }

    public async Task HandleAsync(uSyncImportCompletedNotification notification, CancellationToken cancellationToken)
    {
        await _syncTrackerService.SaveLastSync(notification.Group ?? uSync.EverythingGroupName);
    }
}
