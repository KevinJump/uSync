using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Controllers
{
    [PluginController("uSync")]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
    public partial class uSyncDashboardApiController : UmbracoAuthorizedJsonController
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

        private readonly IConfiguration _configuration;

        /// <summary>
        /// Create a new uSyncDashboardApi Controller (via DI)
        /// </summary>
        public uSyncDashboardApiController(
            IConfiguration configuration,
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

            _configuration = configuration;

        }

        /// <summary>
        ///  Stub - get api used to locate api in umbraco
        /// </summary>
        /// <returns></returns>
        public bool GetApi() => true;

        /// <summary>
        ///  Return the uSyncSettings
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public uSyncSettings GetSettings()
            => this.uSyncConfig.Settings;


        [HttpGet]
        public string GetDefaultSet()
            => this.uSyncConfig.Settings.DefaultSet;

        [HttpGet]
        public IDictionary<string, uSyncHandlerSetSettings> GetSets()
        {
            var section = _configuration.GetSection(uSync.Configuration.uSyncSetsConfig);
            var sets = new Dictionary<string, uSyncHandlerSetSettings>();

            foreach(var item in section.GetChildren())
            {
                sets.Add(item.Key, uSyncConfig.GetSetSettings(item.Key));
            }

            return sets; 
        }

        /// <summary>
        ///  gets the sets that can be picked in the dashboard UI. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSelectableSets()
            => GetSets().Where(x => x.Value.IsSelectable || x.Key.InvariantEquals(uSync.Sets.DefaultSet))
                .Select(x => x.Key);

        /// <summary>
        ///  Return the Handler settings
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public uSyncHandlerSetSettings GetHandlerSetSettings(string id)
            => this.uSyncConfig.GetSetSettings(id);

        /// <summary>
        ///  get a list of loaded handlers 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<object> GetLoadedHandlers()
            => handlerFactory.GetAll();

        /// <summary>
        ///  return handler groups for all enabled handlers
        /// </summary>
        [HttpGet]
        public IDictionary<string, string> GetHandlerGroups(string set)
        {
            var handlerSet = !string.IsNullOrWhiteSpace(set) ? set : uSyncConfig.Settings.DefaultSet;

            var options = new SyncHandlerOptions(handlerSet)
            {
                Group = uSyncConfig.Settings.UIEnabledGroups
            };

            var groups = handlerFactory.GetValidHandlerGroupsAndIcons(options);

            // if nothing is set, we add everything marker.
            if (string.IsNullOrWhiteSpace(uSyncConfig.Settings.UIEnabledGroups) ||
                uSyncConfig.Settings.UIEnabledGroups.InvariantContains("all"))
            {
                groups.Add("_everything", "");
            }

            return groups;
        }

        /// <summary>
        ///  returns the handler groups, even if the handlers
        ///  in them are disabled 
        /// </summary>
        [HttpGet]
        public IEnumerable<string> GetAllHandlerGroups()
        {
            return handlerFactory.GetGroups();
        }

        /// <summary>
        ///  get a list of enabled handlers for the UI
        /// </summary>
        [HttpGet]
        public IEnumerable<SyncHandlerSummary> GetHandlers(string set)
        {
            var handlerSet = !string.IsNullOrWhiteSpace(set) ? set : uSyncConfig.Settings.DefaultSet;

            var options = new SyncHandlerOptions(handlerSet)
            {
                Group = uSyncConfig.Settings.UIEnabledGroups,
            };

            return handlerFactory.GetValidHandlers(options)
                .Select(x => new SyncHandlerSummary()
                {
                    Icon = x.Handler.Icon,
                    Name = x.Handler.Name,
                    Status = HandlerStatus.Pending
                });
        }

        /// <summary>
        ///  run a report on the uSync files
        /// </summary>
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

        /// <summary>
        ///  run an export on the default uSync files
        /// </summary>
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

        /// <summary>
        ///  Run an import on the default uSync files
        /// </summary>

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

        /// <summary>
        ///  Import a single item based on an uSyncAction object
        /// </summary>
        [HttpPut]
        public uSyncAction ImportItem(uSyncAction item)
            => uSyncService.ImportSingleAction(item);

        /// <summary>
        ///  Save the settings to disk 
        /// </summary>
        /// <remarks>
        /// In Umbraco 9 this does not save the settings!
        /// </remarks>
        [HttpPost]
        public void SaveSettings(uSyncSettings settings)
        {
            // uSyncConfig.SaveSettings(settings);
        }

        /// <summary>
        ///  get a JSON representation of the settings as they should appear in appsettings.json file
        /// </summary>
        [HttpGet]
        public JObject GetChangedSettings()
        {

            var settings = JsonConvert.SerializeObject(uSyncConfig.Settings);
            var sets = JsonConvert.SerializeObject(uSyncConfig.GetSetSettings(uSync.Sets.DefaultSet));

            var result = "{ " +
                "\"Settings\" : " + settings + "," + 
                "\"Sets\" : { " +
                    "\"Default\" : " + sets +
                "}" +
            "}";

            return JsonConvert.DeserializeObject<JObject>(result);
        }
    }

    
}
