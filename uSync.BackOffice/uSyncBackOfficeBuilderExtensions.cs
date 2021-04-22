using System;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.WebAssets;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

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
        public static IUmbracoEndpointBuilder UseuSyncEndpoints(this IUmbracoEndpointBuilder app)
        {

            uSyncHubRoutes uSyncHubRoutes = app.ApplicationServices.GetRequiredService<uSyncHubRoutes>();
            uSyncHubRoutes.CreateRoutes(app.EndpointRouteBuilder);
            return app;
        }

        public static IUmbracoBuilder AdduSync(this IUmbracoBuilder builder, Action<uSyncSettings> defaultOptions = default)
        {
            // if the uSyncConfig Service is registred then we assume this has been added before so we don't do it again. 
            if (builder.Services.FirstOrDefault(x => x.ServiceType == typeof(uSyncConfigService)) != null)
                return builder;

            // load up the settings. 
            var options = builder.Services.AddOptions<uSyncSettings>()
                .Bind(builder.Config.GetSection(uSync.Configuration.ConfigSettings));

            if (defaultOptions != default) {
                options.Configure(defaultOptions);
            }
            options.ValidateDataAnnotations();

            // default handler options, other people can load their own names handler options and 
            // they can be used throughout uSync (so complete will do this). 
            var handlerOptiosn = builder.Services.Configure<uSyncHandlerSetSettings>(uSync.Sets.DefaultSet,
                builder.Config.GetSection(uSync.Configuration.ConfigDefaultSet));


            // Setup uSync core.
            builder.Services.AddUnique<uSyncConfigService>();
            builder.Services.AddUnique<uSyncHubRoutes>();

            builder.Services.AddSignalR();

            builder.AdduSyncCore();

            // TODO: we need something here. that lets people add serializers before we then load 
            // the handlers - events/composers won't do, if we are letting people add this as 
            // part of the pipeline. we might need to dynamically load serializers - which is 
            // a pain because they are generic. and then how do we let people unload them ?

            builder.Services.AddUnique<SyncFileService>();

            builder.WithCollectionBuilder<SyncHandlerCollectionBuilder>()
                .Add(() => builder.TypeLoader.GetTypes<ISyncHandler>());

            builder.Services.AddUnique<SyncHandlerFactory>();
            builder.Services.AddUnique<uSyncService>();
            builder.Services.AddUnique<CacheLifecycleManager>();

            // register for the notifications 
            builder.AddNotificationHandler<ServerVariablesParsing, uSyncServerVariablesHandler>();
            builder.AddNotificationHandler<UmbracoApplicationStarting, uSyncApplicationStartingHandler>();
            builder.AddHandlerNotifications();

            return builder;
        }

        internal static void AddHandlerNotifications(this IUmbracoBuilder builder)
        {
            
            // TODO: we need to register all the notifications in the SyncHandlers - not hard wire them here.
            builder.AddNotificationHandler<SavedNotification<IContentType>, ContentTypeHandler>();
            builder.AddNotificationHandler<DeletedNotification<IContentType>, ContentTypeHandler>();
            builder.AddNotificationHandler<MovedNotification<IContentType>, ContentTypeHandler>();
            builder.AddNotificationHandler<EntityContainerSavedNotification, ContentTypeHandler>();

            builder.AddNotificationHandler<SavedNotification<IDataType>, DataTypeHandler>();
            builder.AddNotificationHandler<DeletedNotification<IDataType>, DataTypeHandler>();
            builder.AddNotificationHandler<MovedNotification<IDataType>, DataTypeHandler>();
            builder.AddNotificationHandler<EntityContainerSavedNotification, DataTypeHandler>();

            builder.AddNotificationHandler<SavingNotification<ILanguage>, LanguageHandler>();
            builder.AddNotificationHandler<SavedNotification<ILanguage>, LanguageHandler>();
            builder.AddNotificationHandler<DeletedNotification<ILanguage>, LanguageHandler>();

            builder.AddNotificationHandler<SavedNotification<IMacro>, MacroHandler>();
            builder.AddNotificationHandler<DeletedNotification<IMacro>, MacroHandler>();

            builder.AddNotificationHandler<SavedNotification<IMediaType>, MediaTypeHandler>();
            builder.AddNotificationHandler<DeletedNotification<IMediaType>, MediaTypeHandler>();
            builder.AddNotificationHandler<MovedNotification<IMediaType>, MediaTypeHandler>();
            builder.AddNotificationHandler<EntityContainerSavedNotification, ContentTypeHandler>();

            builder.AddNotificationHandler<SavedNotification<IMemberType>, MemberTypeHandler>();
            builder.AddNotificationHandler<DeletedNotification<IMemberType>, MemberTypeHandler>();
            builder.AddNotificationHandler<MovedNotification<IMemberType>, MemberTypeHandler>();

            builder.AddNotificationHandler<SavedNotification<ITemplate>, TemplateHandler>();
            builder.AddNotificationHandler<DeletedNotification<ITemplate>, TemplateHandler>();
            builder.AddNotificationHandler<MovedNotification<ITemplate>, TemplateHandler>();
        }
    }
}
