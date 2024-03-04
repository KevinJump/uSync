
using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace uSync.AutoTemplates;

public static class AutoTemplatesBuilderExtensions
{
    public static IUmbracoBuilder AdduSyncAutoTemplates(this IUmbracoBuilder builder)
    {
        // check to see if we've been registerd before. 
        if (builder.Services.FindIndex(x => x.ServiceType == typeof(TemplateWatcher)) != -1)
            return builder;

        builder.Services.AddSingleton<TemplateWatcher>();
        builder.AddNotificationHandler<UmbracoApplicationStartingNotification, AutoTemplateNotificationHandler>();
        builder.AddNotificationHandler<TemplateSavingNotification, AutoTemplateNotificationHandler>();

        return builder;

    }
}
