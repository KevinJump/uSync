using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Notifications;
internal class SyncScopedNotificationPublisher
    : ScopedNotificationPublisher<INotificationHandler>
{
    private readonly ILogger<SyncScopedNotificationPublisher> _logger;
    private readonly IEventAggregator _eventAggregator;
    private readonly SyncUpdateCallback _updateCallback;

    public SyncScopedNotificationPublisher(
        IEventAggregator eventAggregator,        
        ILogger<SyncScopedNotificationPublisher> logger,
        SyncUpdateCallback callback)
        : base(eventAggregator, false)
    {
        _eventAggregator = eventAggregator;
        _logger = logger;
        _updateCallback = callback;
    }

    protected override void PublishScopedNotifications(IList<INotification> notifications)
    {
        _logger.LogDebug("Publishing Notification [{count}]", notifications.Count);
        _updateCallback?.Invoke($"Processing notifications [{notifications.Count}]", 9, 10);

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
