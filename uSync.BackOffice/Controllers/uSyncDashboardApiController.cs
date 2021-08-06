using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Controllers
{
    [PluginController("uSync")]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
    public partial class uSyncDashboardApiController : UmbracoAuthorizedApiController
    {

        private readonly AppCaches appCaches;
        private readonly IWebHostEnvironment hostEnvironment;
        private readonly ILogger<uSyncDashboardApiController> logger;
        private readonly ILocalizedTextService textService;

        private readonly uSyncService uSyncService;
        private readonly SyncHandlerFactory handlerFactory;
        private readonly IHubContext<SyncHub> hubContext;

        private uSyncConfigService uSyncConfig;

        private readonly ITypeFinder typeFinder;

        public uSyncDashboardApiController(
            AppCaches appCaches,
            IWebHostEnvironment hostEnvironment,
            ILocalizedTextService textService,
            ILogger<uSyncDashboardApiController> logger,
            ITypeFinder typeFinder,
            uSyncService uSyncService,
            SyncHandlerFactory syncHandlerFactory,
            IHubContext<SyncHub> hubContext,
            uSyncConfigService uSyncConfig)
        {
            this.appCaches = appCaches;
            this.hostEnvironment = hostEnvironment;
            this.textService = textService;
            this.logger = logger;

            this.typeFinder = typeFinder;

            this.uSyncService = uSyncService;
            this.handlerFactory = syncHandlerFactory;
            this.hubContext = hubContext;

            this.uSyncConfig = uSyncConfig;

        }

        public bool GetApi() => true;

        [HttpGet]
        public uSyncSettings GetSettings()
            => this.uSyncConfig.Settings;

        [HttpGet]
        public uSyncHandlerSetSettings GetHandlerSetSettings(string id)
            => this.uSyncConfig.GetSetSettings(id);

        [HttpGet]
        public IEnumerable<object> GetLoadedHandlers()
            => handlerFactory.GetAll();

        /// <summary>
        ///  return handler groups for all enabled handlers
        /// </summary>
        [HttpGet]
        public IDictionary<string, string> GetHandlerGroups()
        {
            var options = new SyncHandlerOptions(uSyncConfig.Settings.DefaultSet)
            {
                Group = uSyncConfig.Settings.UIEnabledGroups
            };

            return handlerFactory.GetValidGroups(options)
                       .ToDictionary(k => k, v => uSyncConstants.Groups.Icons[v]);
        }

        /// <summary>
        ///  returns the handler groups, even if the handlers
        ///  in them are disabled 
        /// </summary>
        [HttpGet]
        public IEnumerable<string> GetAllHandlerGroups()
            => handlerFactory.GetGroups();

        [HttpGet]
        public IEnumerable<SyncHandlerSummary> GetHandlers()
        {
            var options = new SyncHandlerOptions
            {
                Group = uSyncConfig.Settings.UIEnabledGroups
            };

            return handlerFactory.GetValidHandlers(options)
                .Select(x => new SyncHandlerSummary()
                {
                    Icon = x.Handler.Icon,
                    Name = x.Handler.Name,
                    Status = HandlerStatus.Pending
                });
        }

        [HttpPost]
        public IEnumerable<uSyncAction> Report(uSyncOptions options)
        {
            var hubClient = new HubClientService(hubContext, options.ClientId);



            return uSyncService.Report(uSyncConfig.GetRootFolder(), new SyncHandlerOptions()
            {
                Group = options.Group
            },
            hubClient.Callbacks());
        }

        [HttpPost]
        public IEnumerable<uSyncAction> Export(uSyncOptions options)
        {
            var hubClient = new HubClientService(hubContext, options.ClientId);

            if (options.Clean)
            {
                uSyncService.CleanExportFolder(uSyncConfig.GetRootFolder());
            }

            return uSyncService.Export(uSyncConfig.GetRootFolder(), new SyncHandlerOptions()
            {
                Group = options.Group
            },
            hubClient.Callbacks());
        }

        [HttpPut]
        public IEnumerable<uSyncAction> Import(uSyncOptions options)
        {
            var hubClient = new HubClientService(hubContext, options.ClientId);
            return uSyncService.Import(uSyncConfig.GetRootFolder(), options.Force, new SyncHandlerOptions()
            {
                Group = options.Group
            },
            callbacks: hubClient.Callbacks());
        }

        [HttpPut]
        public uSyncAction ImportItem(uSyncAction item)
            => uSyncService.ImportSingleAction(item);


        [HttpPost]
        public void SaveSettings(uSyncSettings settings)
        {
            // uSyncConfig.SaveSettings(settings);
        }
    }
}
