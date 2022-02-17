using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;
using System.Reflection.PortableExecutable;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.BackOffice.Authorization;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

using uSync.BackOffice.Authorization;
using uSync.BackOffice.Cache;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.BackOffice.Notifications;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;

namespace uSync.BackOffice
{
    public static class uSyncBackOfficeBuilderExtensions
    {
        public static IUmbracoBuilder AdduSync(this IUmbracoBuilder builder, Action<uSyncSettings> defaultOptions = default)
        {
            // if the uSyncConfig Service is registred then we assume this has been added before so we don't do it again. 
            if (builder.Services.FirstOrDefault(x => x.ServiceType == typeof(uSyncConfigService)) != null)
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
            var handlerOptiosn = builder.Services.Configure<uSyncHandlerSetSettings>(uSync.Sets.DefaultSet,
                builder.Config.GetSection(uSync.Configuration.ConfigDefaultSet));


            // Setup uSync core.
            builder.AdduSyncCore();


            // Setup the back office.
            builder.Services.AddSingleton<uSyncEventService>();
            builder.Services.AddSingleton<uSyncConfigService>();
            builder.Services.AddSingleton<SyncFileService>();

            builder.WithCollectionBuilder<SyncHandlerCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<ISyncHandler>());

            builder.Services.AddSingleton<SyncHandlerFactory>();
            builder.Services.AddSingleton<uSyncService>();
            builder.Services.AddSingleton<CacheLifecycleManager>();

            // register for the notifications 
            builder.AddNotificationHandler<ServerVariablesParsingNotification, uSyncServerVariablesHandler>();
            builder.AddNotificationHandler<UmbracoApplicationStartingNotification, uSyncApplicationStartingHandler>();
            builder.AddHandlerNotifications();

            builder.Services.AddSingleton<uSyncHubRoutes>();
            builder.Services.AddSignalR();
            builder.Services.AdduSyncSignalR();

            builder.Services.AddAuthorization(o => CreatePolicies(o));

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
                    "uSync",
                    applicationBuilder => { },
                    applicationBuilder => { },
                    applicationBuilder =>
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

            builder.AddNotificationHandler<DataTypeSavedNotification, DataTypeHandler>();
            builder.AddNotificationHandler<DataTypeDeletedNotification, DataTypeHandler>();
            builder.AddNotificationHandler<DataTypeMovedNotification, DataTypeHandler>();
            builder.AddNotificationHandler<EntityContainerSavedNotification, DataTypeHandler>();

            builder.AddNotificationHandler<ContentTypeSavedNotification, ContentTypeHandler>();
            builder.AddNotificationHandler<ContentTypeDeletedNotification, ContentTypeHandler>();
            builder.AddNotificationHandler<ContentTypeMovedNotification, ContentTypeHandler>();
            builder.AddNotificationHandler<EntityContainerSavedNotification, ContentTypeHandler>();

            builder.AddNotificationHandler<MediaTypeSavedNotification, MediaTypeHandler>();
            builder.AddNotificationHandler<MediaTypeDeletedNotification, MediaTypeHandler>();
            builder.AddNotificationHandler<MediaTypeMovedNotification, MediaTypeHandler>();
            builder.AddNotificationHandler<EntityContainerSavedNotification, MediaTypeHandler>();

            builder.AddNotificationHandler<MemberTypeSavedNotification, MemberTypeHandler>();
            builder.AddNotificationHandler<MemberTypeSavedNotification, MemberTypeHandler>();
            builder.AddNotificationHandler<MemberTypeMovedNotification, MemberTypeHandler>();

            builder.AddNotificationHandler<LanguageSavingNotification, LanguageHandler>();
            builder.AddNotificationHandler<LanguageSavedNotification, LanguageHandler>();
            builder.AddNotificationHandler<LanguageDeletedNotification, LanguageHandler>();

            builder.AddNotificationHandler<MacroSavedNotification, MacroHandler>();
            builder.AddNotificationHandler<MacroDeletedNotification, MacroHandler>();

            builder.AddNotificationHandler<TemplateSavedNotification, TemplateHandler>();
            builder.AddNotificationHandler<TemplateDeletedNotification, TemplateHandler>();

            // content ones
            builder.AddNotificationHandler<ContentSavedNotification, ContentHandler>();
            builder.AddNotificationHandler<ContentDeletedNotification, ContentHandler>();
            builder.AddNotificationHandler<ContentMovedNotification, ContentHandler>();
            builder.AddNotificationHandler<ContentMovedToRecycleBinNotification, ContentHandler>();

            builder.AddNotificationHandler<MediaSavedNotification, MediaHandler>();
            builder.AddNotificationHandler<MediaDeletedNotification, MediaHandler>();
            builder.AddNotificationHandler<MediaMovedNotification, MediaHandler>();
            builder.AddNotificationHandler<MediaMovedToRecycleBinNotification, MediaHandler>();

            builder.AddNotificationHandler<DomainSavedNotification, DomainHandler>();
            builder.AddNotificationHandler<DomainDeletedNotification, DomainHandler>();

            builder.AddNotificationHandler<DictionaryItemSavedNotification, DictionaryHandler>();
            builder.AddNotificationHandler<DictionaryItemDeletedNotification, DictionaryHandler>();

            builder.AddNotificationHandler<RelationTypeSavedNotification, RelationTypeHandler>();
            builder.AddNotificationHandler<RelationTypeDeletedNotification, RelationTypeHandler>();

            // builder.AddNotificationHandler<ContentSavedBlueprintNotification, ContentHandler>();
            // builder.AddNotificationHandler<ContentDeletedBlueprintNotification, ContentHandler>();

            // cache lifecylce manager
            builder.
                AddNotificationHandler<uSyncImportStartingNotification, CacheLifecycleManager>().
                AddNotificationHandler<uSyncReportStartingNotification, CacheLifecycleManager>().
                AddNotificationHandler<uSyncExportStartingNotification, CacheLifecycleManager>().
                AddNotificationHandler<uSyncImportCompletedNotification, CacheLifecycleManager>().
                AddNotificationHandler<uSyncReportCompletedNotification, CacheLifecycleManager>().
                AddNotificationHandler<uSyncExportCompletedNotification, CacheLifecycleManager>().
                AddNotificationHandler<ContentSavingNotification, CacheLifecycleManager>().
                AddNotificationHandler<ContentDeletingNotification, CacheLifecycleManager>().
                AddNotificationHandler<ContentMovingNotification, CacheLifecycleManager>().
                AddNotificationHandler<MediaSavingNotification, CacheLifecycleManager>().
                AddNotificationHandler<MediaSavedNotification, CacheLifecycleManager>().
                AddNotificationHandler<MediaDeletedNotification, CacheLifecycleManager>();
        }


        private static void CreatePolicies(AuthorizationOptions options,
            string backofficeAuthenticationScheme = Constants.Security.BackOfficeAuthenticationType)
        {
            options.AddPolicy(SyncAuthorizationPolicies.TreeAccessuSync, policy =>
            {
                policy.AuthenticationSchemes.Add(backofficeAuthenticationScheme);
                policy.Requirements.Add(new TreeRequirement(uSync.Trees.uSync));
            });
        }
    }
}
