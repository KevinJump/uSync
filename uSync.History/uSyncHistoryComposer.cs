using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uSync.BackOffice;

namespace uSync.History
{
    public class uSyncHistoryComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationHandler<uSyncImportCompletedNotification, uSyncHistoryNotificationHandler>();
            builder.AddNotificationHandler<uSyncExportCompletedNotification, uSyncHistoryNotificationHandler>();
            // don't add if the filter is already there .
            if (!builder.ManifestFilters().Has<uSyncHistoryManifestFilter>())
            {
                // add the package manifest programatically. 
                builder.ManifestFilters().Append<uSyncHistoryManifestFilter>();
            }
        }
    }
}
