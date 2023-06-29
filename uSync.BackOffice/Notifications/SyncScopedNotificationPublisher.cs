using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace uSync.BackOffice.Notifications;
internal class SyncScopedNotificationPublisher
    : ScopedNotificationPublisher<IDistributedCacheNotificationHandler>
{
    private ILogger<uSyncService> _logger;
    private IEventAggregator _eventAggregator;

    public SyncScopedNotificationPublisher(
        IEventAggregator eventAggregator,        
        ILogger<uSyncService> logger)
        : base(eventAggregator, false)
    {
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

    protected override void PublishScopedNotifications(IList<INotification> notifications)
    {
        _logger.LogDebug("Publishing Notification.. {count}", notifications.Count);


        var grouped = notifications
            .Where(x => x != null)
            .GroupBy(x => x.GetType().Name);

        foreach(var n in grouped)
        {
            _logger.LogDebug("Push: {x} {count}", n.Key, n.Count());
            _eventAggregator.Publish(n);
        }
    }
}
