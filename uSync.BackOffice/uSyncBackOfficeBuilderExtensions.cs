using System;
using System.Linq;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

using uSync.BackOffice.Authorization;
using uSync.BackOffice.Boot;
using uSync.BackOffice.Cache;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.BackOffice.Legacy;
using uSync.BackOffice.Notifications;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.Core;

namespace uSync.BackOffice;

/// <summary>
///  extensions to the IUmbracoBuilder object to add uSync to a site.
/// </summary>
public static class uSyncBackOfficeBuilderExtensions
{
    /// <summary>
    ///  Add uSync to the site. 
    /// </summary>
    public static IUmbracoBuilder AdduSync(this IUmbracoBuilder builder, Action<uSyncSettings>? defaultOptions = null)
    {
        // if the uSyncConfig Service is registered then we assume this has been added before so we don't do it again. 
        if (builder.Services.FirstOrDefault(x => x.ServiceType == typeof(ISyncConfigService)) != null)
            return builder;

        // load up the settings. 
        var options = builder.Services.AddOptions<uSyncSettings>()
            .Bind(builder.Config.GetSection(uSync.Configuration.ConfigSettings));

        if (defaultOptions != default)
        {
            options.Configure(defaultOptions);
        }
        options.ValidateDataAnnotations();

        // default handler options, other people can load their own names handler options and 
        // they can be used throughout uSync (so complete will do this). 
        var handlerOptions = builder.Services.Configure<uSyncHandlerSetSettings>(uSync.Sets.DefaultSet,
            builder.Config.GetSection(uSync.Configuration.ConfigDefaultSet));


        // Setup uSync core.
        builder.AdduSyncCore();


        // Setup the back office.
        builder.Services.AddSingleton<ISyncEventService, SyncEventService>();
        builder.Services.AddSingleton<ISyncConfigService, SyncConfigService>();
        builder.Services.AddSingleton<ISyncFileService, SyncFileService>();

        builder.WithCollectionBuilder<SyncHandlerCollectionBuilder>()
            .Add(() => builder.TypeLoader.GetTypes<ISyncHandler>());

        builder.Services.AddSingleton<SyncHandlerFactory>();
        builder.Services.AddSingleton<ISyncService, SyncService>();
        builder.Services.AddSingleton<CacheLifecycleManager>();

        // first boot should happen before any other bits of uSync export on a blank site. 
        builder.AdduSyncFirstBoot();

        // register for the notifications 
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, uSyncApplicationStartingHandler>();
        builder.AddHandlerNotifications();

        builder.Services.AddSingleton<uSyncHubRoutes>();
        builder.Services.AddSignalR();
        builder.Services.AdduSyncSignalR();


        builder.Services.AddTransient<ISyncLegacyService, SyncLegacyService>();

        builder.Services.AddSingleton<IAuthorizationHandler, uSyncAllowedApplicationHandler>();
        builder.Services.AddAuthorization(o => CreatePolicies(o));

        builder.Services.AddTransient<ISyncActionService, SyncActionService>();

        _ = builder.Services.PostConfigure<uSyncSettings>(options =>
        {
            if (options.Folders == null || options.Folders.Length == 0)
            {
                options.Folders = ["uSync/Root/", options.RootFolder];
            }
        });

        return builder;
    }

    /// <summary>
    ///  Adds the signalR hub route for uSync
    /// </summary>
    public static IServiceCollection AdduSyncSignalR(this IServiceCollection services)
    {
        services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(
                "uSync", endpoints: applicationBuilder =>
                {
                    applicationBuilder.UseEndpoints(e =>
                    {
                        var hubRoutes = applicationBuilder.ApplicationServices.GetRequiredService<uSyncHubRoutes>();
                        hubRoutes.CreateRoutes(e);
                    });
                }
                ));
        });

        return services;
    }

    internal static void AddHandlerNotifications(this IUmbracoBuilder builder)
    {

        // TODO: Would be nice if we could just register all the notifications in the handlers
        builder.AddNotificationAsyncHandler<DataTypeSavedNotification, DataTypeHandler>();
        builder.AddNotificationAsyncHandler<DataTypeDeletedNotification, DataTypeHandler>();
        builder.AddNotificationAsyncHandler<DataTypeMovedNotification, DataTypeHandler>();
        builder.AddNotificationAsyncHandler<EntityContainerSavedNotification, DataTypeHandler>();
        builder.AddNotificationAsyncHandler<EntityContainerRenamedNotification, DataTypeHandler>();

        builder.AddNotificationAsyncHandler<ContentTypeSavedNotification, ContentTypeHandler>();
        builder.AddNotificationAsyncHandler<ContentTypeDeletedNotification, ContentTypeHandler>();
        builder.AddNotificationAsyncHandler<ContentTypeMovedNotification, ContentTypeHandler>();
        builder.AddNotificationAsyncHandler<EntityContainerSavedNotification, ContentTypeHandler>();
        builder.AddNotificationAsyncHandler<EntityContainerRenamedNotification, ContentTypeHandler>();

        builder.AddNotificationAsyncHandler<MediaTypeSavedNotification, MediaTypeHandler>();
        builder.AddNotificationAsyncHandler<MediaTypeDeletedNotification, MediaTypeHandler>();
        builder.AddNotificationAsyncHandler<MediaTypeMovedNotification, MediaTypeHandler>();
        builder.AddNotificationAsyncHandler<EntityContainerSavedNotification, MediaTypeHandler>();
        builder.AddNotificationAsyncHandler<EntityContainerRenamedNotification, MediaTypeHandler>();

        builder.AddNotificationAsyncHandler<MemberTypeSavedNotification, MemberTypeHandler>();
        builder.AddNotificationAsyncHandler<MemberTypeSavedNotification, MemberTypeHandler>();
        builder.AddNotificationAsyncHandler<MemberTypeMovedNotification, MemberTypeHandler>();
        builder.AddNotificationAsyncHandler<EntityContainerSavedNotification, MemberTypeHandler>();
        builder.AddNotificationAsyncHandler<EntityContainerRenamedNotification, MemberTypeHandler>();

        builder.AddNotificationAsyncHandler<LanguageSavingNotification, LanguageHandler>();
        builder.AddNotificationAsyncHandler<LanguageSavedNotification, LanguageHandler>();
        builder.AddNotificationAsyncHandler<LanguageDeletedNotification, LanguageHandler>();

        //builder.AddNotificationAsyncHandler<MacroSavedNotification, MacroHandler>();
        //builder.AddNotificationAsyncHandler<MacroDeletedNotification, MacroHandler>();

        builder.AddNotificationAsyncHandler<TemplateSavedNotification, TemplateHandler>();
        builder.AddNotificationAsyncHandler<TemplateDeletedNotification, TemplateHandler>();

        builder.AddNotificationAsyncHandler<WebhookSavedNotification, WebhookHandler>();
        builder.AddNotificationAsyncHandler<WebhookDeletedNotification, WebhookHandler>();

        // roots - pre-notifications for stopping things
        builder
            .AddNotificationAsyncHandler<ContentTypeSavingNotification, ContentTypeHandler>()
            .AddNotificationAsyncHandler<ContentTypeDeletingNotification, ContentTypeHandler>()
            .AddNotificationAsyncHandler<ContentTypeMovingNotification, ContentTypeHandler>()

            .AddNotificationAsyncHandler<MediaTypeSavingNotification, MediaTypeHandler>()
            .AddNotificationAsyncHandler<MediaTypeDeletingNotification, MediaTypeHandler>()
            .AddNotificationAsyncHandler<MediaTypeMovingNotification, MediaTypeHandler>()

            .AddNotificationAsyncHandler<MemberTypeSavingNotification, MemberTypeHandler>()
            .AddNotificationAsyncHandler<MemberTypeDeletingNotification, MemberTypeHandler>()
            .AddNotificationAsyncHandler<MemberTypeMovingNotification, MemberTypeHandler>()

            .AddNotificationAsyncHandler<DataTypeSavingNotification, DataTypeHandler>()
            .AddNotificationAsyncHandler<DataTypeDeletingNotification, DataTypeHandler>()
            .AddNotificationAsyncHandler<DataTypeMovingNotification, DataTypeHandler>()

            .AddNotificationAsyncHandler<ContentSavingNotification, ContentHandler>()
            .AddNotificationAsyncHandler<ContentDeletingNotification, ContentHandler>()
            .AddNotificationAsyncHandler<ContentMovingNotification, ContentHandler>()

            .AddNotificationAsyncHandler<MediaSavingNotification, MediaHandler>()
            .AddNotificationAsyncHandler<MediaDeletingNotification, MediaHandler>()
            .AddNotificationAsyncHandler<MediaMovingNotification, MediaHandler>()

            .AddNotificationAsyncHandler<DictionaryItemSavingNotification, DictionaryHandler>()
            .AddNotificationAsyncHandler<DictionaryItemDeletingNotification, DictionaryHandler>()

            .AddNotificationAsyncHandler<RelationTypeSavingNotification, RelationTypeHandler>()
            .AddNotificationAsyncHandler<RelationTypeDeletingNotification, RelationTypeHandler>()


            .AddNotificationAsyncHandler<TemplateSavingNotification, TemplateHandler>()
            .AddNotificationAsyncHandler<TemplateDeletingNotification, TemplateHandler>()

            .AddNotificationAsyncHandler<WebhookSavingNotification, WebhookHandler>()
            .AddNotificationAsyncHandler<WebhookDeletingNotification, WebhookHandler>();


        // content ones
        builder.AddNotificationAsyncHandler<ContentSavedNotification, ContentHandler>();
        builder.AddNotificationAsyncHandler<ContentDeletedNotification, ContentHandler>();
        builder.AddNotificationAsyncHandler<ContentMovedNotification, ContentHandler>();
        builder.AddNotificationAsyncHandler<ContentMovedToRecycleBinNotification, ContentHandler>();

        builder.AddNotificationAsyncHandler<MediaSavedNotification, MediaHandler>();
        builder.AddNotificationAsyncHandler<MediaDeletedNotification, MediaHandler>();
        builder.AddNotificationAsyncHandler<MediaMovedNotification, MediaHandler>();
        builder.AddNotificationAsyncHandler<MediaMovedToRecycleBinNotification, MediaHandler>();

        builder.AddNotificationAsyncHandler<DomainSavedNotification, DomainHandler>();
        builder.AddNotificationAsyncHandler<DomainDeletedNotification, DomainHandler>();

        builder.AddNotificationAsyncHandler<DictionaryItemSavedNotification, DictionaryHandler>();
        builder.AddNotificationAsyncHandler<DictionaryItemDeletedNotification, DictionaryHandler>();

        builder.AddNotificationAsyncHandler<RelationTypeSavedNotification, RelationTypeHandler>();
        builder.AddNotificationAsyncHandler<RelationTypeDeletedNotification, RelationTypeHandler>();

        builder.AddNotificationAsyncHandler<ContentSavedBlueprintNotification, ContentTemplateHandler>();
        builder.AddNotificationAsyncHandler<ContentDeletedBlueprintNotification, ContentTemplateHandler>();

        // cache lifecycle manager
        builder.
            AddNotificationAsyncHandler<uSyncImportStartingNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<uSyncReportStartingNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<uSyncExportStartingNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<uSyncImportCompletedNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<uSyncReportCompletedNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<uSyncExportCompletedNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<ContentSavingNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<ContentDeletingNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<ContentMovingNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<MediaSavingNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<MediaSavedNotification, CacheLifecycleManager>().
            AddNotificationAsyncHandler<MediaDeletedNotification, CacheLifecycleManager>();
    }


    private static void CreatePolicies(AuthorizationOptions options,
        string backOfficeAuthScheme = Constants.Security.BackOfficeAuthenticationType)
    {
        options.AddPolicy(SyncAuthorizationPolicies.TreeAccessuSync, policy =>
        {
            policy.AuthenticationSchemes.Add(backOfficeAuthScheme);
            policy.Requirements.Add(new uSyncApplicationRequirement(Constants.Applications.Settings));
        });
    }
}
