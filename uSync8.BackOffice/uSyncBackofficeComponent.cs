using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.JavaScript;

using uSync8.BackOffice.Cache;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Controllers;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{
    public class uSyncBackofficeComponent : IComponent
    {
        private readonly IProfilingLogger logger;
        private readonly SyncHandlerFactory handlerFactory;

        private readonly SyncFileService syncFileService;
        private readonly uSyncSettings uSyncSettings;
        private readonly uSyncService uSyncService;
        private readonly CacheLifecycleManager cacheLifecycleManager;
        private readonly IRuntimeState runtimeState;

        private readonly IUmbracoContextFactory umbracoContextFactory;

        private readonly string UmbracoMvcArea;

        public uSyncBackofficeComponent(
            IGlobalSettings globalSettings,
            uSyncConfig uSyncConfig,
            SyncHandlerFactory handlerFactory,
            IProfilingLogger logger,
            SyncFileService fileService,
            uSyncService uSyncService,
            CacheLifecycleManager cacheLifecycleManager,
            IRuntimeState runtimeState,
            IUmbracoContextFactory umbracoContextFactory)
        {
            uSyncSettings = uSyncConfig.Settings;

            UmbracoMvcArea = globalSettings.GetUmbracoMvcArea();

            this.runtimeState = runtimeState;
            this.logger = logger;

            this.handlerFactory = handlerFactory;

            this.syncFileService = fileService;
            this.uSyncService = uSyncService;
            this.cacheLifecycleManager = cacheLifecycleManager;

            this.umbracoContextFactory = umbracoContextFactory;

        }

        public void Initialize()
        {
            ServerVariablesParser.Parsing += ServerVariablesParser_Parsing;

            if (runtimeState.Level < RuntimeLevel.Run)
            {
                logger.Info<uSyncBackofficeComponent>("Umbraco is not in Run Mode {0} so uSync is not going to run", runtimeState.Level);
                return;
            }

            if (runtimeState.ServerRole == Umbraco.Core.Sync.ServerRole.Replica)
            {
                logger.Info<uSyncBackofficeComponent>("This is a replica server, uSync will not run any of the startup events");
                return;
            }

            using (logger.DebugDuration<uSyncBackofficeComponent>("uSync Starting"))
            {
                InitBackOffice();
            }

        }

        private void ServerVariablesParser_Parsing(object sender, Dictionary<string, object> e)
        {
            if (HttpContext.Current == null)
                throw new InvalidOperationException("This method requires that an HttpContext be active");

            var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

            e.Add("uSync", new Dictionary<string, object>
            {
                { "uSyncService", urlHelper.GetUmbracoApiServiceBaseUrl<uSyncDashboardApiController>(controller => controller.GetApi()) },
                { "signalRHub", UriUtility.ToAbsolute($"~/{UmbracoMvcArea}/{uSyncSettings.SignalRRoot}") },
                { "isLoadBalanced", runtimeState.ServerRole == Umbraco.Core.Sync.ServerRole.Replica }
            });
        }

        private void InitBackOffice()
        {
            var sw = Stopwatch.StartNew();

            try
            {

                using (var reference = umbracoContextFactory.EnsureUmbracoContext())
                {
                    cacheLifecycleManager.Intialize();

                    if (uSyncSettings.ExportAtStartup || (uSyncSettings.ExportOnSave && !syncFileService.RootExists(uSyncSettings.RootFolder)))
                    {
                        logger.Info<uSyncBackofficeComponent>("uSync: Running Export at startup");
                        uSyncService.Export(uSyncSettings.RootFolder, default(SyncHandlerOptions));
                    }

                    if (uSyncSettings.ImportAtStartup)
                    {
                        logger.Info<uSyncBackofficeComponent>("uSync: Running Import at startup");

                        if (!HasStopFile(uSyncSettings.RootFolder))
                        {
                            uSyncService.Import(uSyncSettings.RootFolder, false, new SyncHandlerOptions
                            {
                                Group = uSyncSettings.ImportAtStartupGroup
                            });

                            ProcessOnceFile(uSyncSettings.RootFolder);
                        }
                        else
                        {
                            logger.Info<uSyncBackofficeComponent>("Startup Import blocked by usync.stop file");
                        }
                    }

                    if (uSyncSettings.ExportOnSave)
                    {
                        var handlers = handlerFactory
                            .GetValidHandlers(new SyncHandlerOptions(handlerFactory.DefaultSet, HandlerActions.Save))
                            .ToList();

                        logger.Info<uSyncBackofficeComponent>("uSync: Initializing events for {count} Handlers", handlers.Count);

                        foreach (var syncHandler in handlers)
                        {
                            logger.Debug<uSyncBackofficeComponent>($"  Initializing up Handler {syncHandler.Handler.Name}");
                            syncHandler.Handler.Initialize(syncHandler.Settings);
                        }
                    }
                }

                sw.Stop();
                logger.Info<uSyncBackofficeComponent>("uSync: Startup Processes Complete {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.Warn<uSyncBackofficeComponent>($"uSync: Error during at startup {ex.Message}");
            }

        }

        public void Terminate()
        {
            logger.Debug<uSyncBackofficeComponent>("Terminiating Component");
            if (uSyncSettings.ExportOnSave)
            {
                var handlers = handlerFactory
                    .GetValidHandlers(new SyncHandlerOptions(handlerFactory.DefaultSet, HandlerActions.Save))
                    .ToList();

                logger.Info<uSyncBackofficeComponent>("uSync: Cleaning up events for {count} Handlers", handlers.Count);

                foreach (var syncHandler in handlers)
                {
                    if (syncHandler.Handler is ISyncItemHandler itemHandler)
                    {
                        itemHandler.Terminate(syncHandler.Settings);
                    }
                }
            }
        }



        private bool HasStopFile(string folder)
            => syncFileService.FileExists($"{folder}/usync.stop");

        private void ProcessOnceFile(string folder)
        {
            if (syncFileService.FileExists($"{folder}/usync.once"))
            {
                syncFileService.DeleteFile($"{folder}/usync.once");
                syncFileService.SaveFile($"{folder}/usync.stop", "uSync Stop file, prevents startup import");
                logger.Info<uSyncBackofficeComponent>("usync.once file replaced by usync.stop file");
            }
        }
    }
}
