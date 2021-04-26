
using Umbraco.Cms.Core.Events;

namespace uSync.BackOffice.Services
{
    /// <summary>
    ///  handles the events and locks for uSync events. 
    /// </summary>
    /// <remarks>
    ///  stops us tripping up over usync firing save events etc while importing
    ///  gives us one place to fire our notifications from 
    /// </remarks>
    public class uSyncEventService
    {
        private readonly IEventAggregator _eventAggregator;

        public uSyncEventService(IEventAggregator eventAggregator) 
        {
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        ///  is uSync paused or not ? 
        /// </summary>
        public bool IsPaused { get; private set; }


        /// <summary>
        ///  pause the uSync triggering process
        /// </summary>
        public void Pause() => IsPaused = true;

        /// <summary>
        ///  unpause the uSync triggering process
        /// </summary>
        public void UnPause() => IsPaused = false;

        /// <summary>
        ///  get an import pause object (pauses the import until it is disposed)
        /// </summary>
        /// <remarks>
        ///  you should wrap code that might trigger umbraco events in using(var pause = _mutexService.ImportPause())
        ///  this will ensure that uSync doesn't then pickupt the imports as new things and saves them to disk.
        /// </remarks>
        public uSyncImportPause ImportPause()
            => new uSyncImportPause(this);



        //// notification events. 
        ///

        internal bool FireBulkStarting(CancelableuSyncBulkNotification bulkNotification)
        {
            _eventAggregator.PublishCancelable(bulkNotification);
            return bulkNotification.Cancel;
        }

        internal void FireBulkComplete(uSyncBulkNotification notification)
        {
            _eventAggregator.Publish(notification);
        }

        internal bool FireItemStartingEvent<TObject>(CancelableuSyncItemNotification<TObject> notification)
        {
            _eventAggregator.PublishCancelable(notification);
            return notification.Cancel;
        }

        internal void FireItemCompletedEvent<TObject>(uSyncItemNotification<TObject> notification)
        {
            _eventAggregator.Publish(notification);
        }



    }
}
