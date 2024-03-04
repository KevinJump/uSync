using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Notifications;

namespace uSync.AutoTemplates;

public class AutoTemplateNotificationHandler :
    INotificationHandler<UmbracoApplicationStartingNotification>,
    INotificationHandler<TemplateSavingNotification>
{

    private readonly IHostingEnvironment _hostingEnvironment;
    private readonly TemplateWatcher _templateWatcher;

    public AutoTemplateNotificationHandler(
        IHostingEnvironment hostingEnvironment,
        TemplateWatcher templateWatcher)
    {
        _hostingEnvironment = hostingEnvironment;
        _templateWatcher = templateWatcher;
    }

    public void Handle(UmbracoApplicationStartingNotification notification)
    {
        // we only run in debug mode. 
        if (!_hostingEnvironment.IsDebugMode) return;

        // we only run when Umbraco is setup.
        if (notification.RuntimeLevel == Umbraco.Cms.Core.RuntimeLevel.Run)
        {
            _templateWatcher.CheckViewsFolder();
            _templateWatcher.WatchViewsFolder();
        }
    }

    public void Handle(TemplateSavingNotification notification)
    {
        foreach (var item in notification.SavedEntities)
        {
            // tells the watcher this has been saved in umbraco.
            _templateWatcher.QueueChange(item.Alias);
        }
    }
}
