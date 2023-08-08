using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Extensions.Logging;

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
        // checking count means we don't send signalR messages when we don't need to.
        if (notifications.Count > 0)
        {
            //_updateCallback?.Invoke($"Processing notifications [{notifications.Count}]", 9, 10);
            _logger.LogDebug(">> Publishing Notifications [{count}]", notifications.Count);
            var sw = Stopwatch.StartNew();

            var groupedNotifications = notifications
                .Where(x => x != null)
                .GroupBy(x => x.GetType().Name);

            foreach (var items in groupedNotifications)
            {
                _updateCallback?.Invoke($"Processing {items.Key}s ({items.Count()})", 90, 100);
                _eventAggregator.Publish(items);
            }

            sw.Stop();
            _logger.LogDebug("<< Notifications processed - {elapsed}ms", sw.ElapsedMilliseconds);

            if (sw.ElapsedMilliseconds / notifications.Count > 2000)
                _logger.LogWarning("Processing notifications is slow, you should check for custom code running on notification events that may slow this down");
        }
    }
}
