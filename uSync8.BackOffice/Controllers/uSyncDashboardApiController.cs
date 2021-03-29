using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

using Umbraco.Core.Composing;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Hubs;
using uSync8.BackOffice.SyncHandlers;

using Constants = Umbraco.Core.Constants;

namespace uSync8.BackOffice.Controllers
{
    [PluginController("uSync")]
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
    public partial class uSyncDashboardApiController : UmbracoAuthorizedApiController
    {
        private readonly uSyncService uSyncService;
        private readonly SyncHandlerFactory handlerFactory;

        private uSyncSettings settings;
        private uSyncConfig Config;

        public uSyncDashboardApiController(
            uSyncService uSyncService,
            SyncHandlerFactory handlerFactory,
            uSyncConfig config)
        {
            this.Config = config;
            this.uSyncService = uSyncService;

            this.settings = Current.Configs.uSync();
            this.handlerFactory = handlerFactory;

            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;
        }

        public bool GetApi() => true;

        private void BackOfficeConfig_Reloaded(uSyncSettings settings)
        {
            this.settings = settings;
        }

        [HttpGet]
        public uSyncSettings GetSettings()
            => settings;

        [HttpGet]
        public IEnumerable<object> GetLoadedHandlers()
            => handlerFactory.GetAll();

        /// <summary>
        ///  return handler groups for all enabled handlers
        /// </summary>
        [HttpGet]
        public IEnumerable<string> GetHandlerGroups()
            => handlerFactory.GetValidGroups(new SyncHandlerOptions(uSync.Handlers.DefaultSet));

        /// <summary>
        ///  returns the handler groups, even if the handlers
        ///  in them are disabled 
        /// </summary>
        [HttpGet]
        public IEnumerable<string> GetAllHandlerGroups()
            => handlerFactory.GetGroups();

        [HttpGet]
        public IEnumerable<object> GetHandlers()
            => handlerFactory.GetAll()
                .Select(x => new SyncHandlerSummary()
                {
                    Icon = x.Icon,
                    Name = x.Name,
                    Status = HandlerStatus.Pending
                });

        [HttpPost]
        public IEnumerable<uSyncAction> Report(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);
            return uSyncService.Report(settings.RootFolder, new SyncHandlerOptions()
            {
                Group = options.Group
            },
            hubClient.Callbacks());
        }

        [HttpPost]
        public IEnumerable<uSyncAction> Export(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);

            if (options.Clean)
            {
                uSyncService.CleanExportFolder(settings.RootFolder);
            }

            return uSyncService.Export(settings.RootFolder, new SyncHandlerOptions()
            {
                Group = options.Group
            },
            hubClient.Callbacks());
        }

        [HttpPut]
        public IEnumerable<uSyncAction> Import(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);
            return uSyncService.Import(settings.RootFolder, options.Force, new SyncHandlerOptions()
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
            Config.SaveSettings(settings, true);
        }
    }
}
