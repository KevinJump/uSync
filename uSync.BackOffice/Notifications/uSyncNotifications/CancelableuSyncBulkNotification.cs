using System.Linq;

using Umbraco.Cms.Core.Notifications;

namespace uSync.BackOffice
{
    public class CancelableuSyncBulkNotification : uSyncBulkNotification, ICancelableNotification
    {
        public CancelableuSyncBulkNotification() 
            : base(Enumerable.Empty<uSyncAction>()) 
        { }

        public bool Cancel { get; set; }
    }


}
