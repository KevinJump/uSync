using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.JavaScript;

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
        private readonly uSyncSettings globalSettings;
        private readonly uSyncService uSyncService;
        private readonly IRuntimeState runtimeState;

        private readonly IUmbracoContextFactory umbracoContextFactory;

        public uSyncBackofficeComponent(
            SyncHandlerFactory handlerFactory,
            IProfilingLogger logger,
            SyncFileService fileService,
            uSyncService uSyncService,
            IRuntimeState runtimeState,
            IUmbracoContextFactory umbracoContextFactory)
        {
            globalSettings = Current.Configs.uSync();

            this.runtimeState = runtimeState;
            this.logger = logger;

            this.handlerFactory = handlerFactory;

            this.syncFileService = fileService;
            this.uSyncService = uSyncService;

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
                { "uSyncService", urlHelper.GetUmbracoApiServiceBaseUrl<uSyncDashboardApiController>(controller => controller.GetApi()) }
            });
        }

        private void InitBackOffice()
        {
            try
            {

                using (var reference = umbracoContextFactory.EnsureUmbracoContext())
                {

                    if (globalSettings.ExportAtStartup || (globalSettings.ExportOnSave && !syncFileService.RootExists(globalSettings.RootFolder)))
                    {
                        uSyncService.Export(globalSettings.RootFolder, default(SyncHandlerOptions));
                    }

                    if (globalSettings.ImportAtStartup)
                    {
                        if (!HasStopFile(globalSettings.RootFolder))
                        {
                            uSyncService.Import(globalSettings.RootFolder, false, default(SyncHandlerOptions));
                            ProcessOnceFile(globalSettings.RootFolder);
                        }
                        else
                        {
                            logger.Info<uSyncBackofficeComponent>("Startup Import blocked by usync.stop file");
                        }
                    }

                    if (globalSettings.ExportOnSave)
                    {
                        var handlers = handlerFactory.GetValidHandlers(new SyncHandlerOptions(handlerFactory.DefaultSet, HandlerActions.Save));

                        foreach (var syncHandler in handlers)
                        {
                            logger.Debug<uSyncBackofficeComponent>($"Starting up Handler {syncHandler.Handler.Name}");
                            syncHandler.Handler.Initialize(syncHandler.Settings);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn<uSyncBackofficeComponent>($"Error Importing at startup {ex.Message}");
            }

        }

        public void Terminate()
        {
            logger.Debug<uSyncBackofficeComponent>("Terminiating Component");
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
