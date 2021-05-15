using Umbraco.Cms.Core.Notifications;

using uSync.Core;

namespace uSync.BackOffice
{
    public class uSyncItemNotification<TObject> : INotification
    {
        public uSyncItemNotification(TObject item)
        {
            this.Item = item;
        }
        public uSyncItemNotification(TObject item, ChangeType change)
            : this(item)
        {
            this.Change = change;
        }

        public ChangeType Change { get; set; }

        public TObject Item { get; set; }
    }


}
