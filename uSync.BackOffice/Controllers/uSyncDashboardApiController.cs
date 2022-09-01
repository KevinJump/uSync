using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.BackOffice.Services;
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
        private readonly SyncFileService _syncFileService;
        private readonly SyncHandlerFactory handlerFactory;
        private readonly IHubContext<SyncHub> hubContext;

        private uSyncConfigService uSyncConfig;

        private readonly ITypeFinder typeFinder;

        private readonly IConfiguration _configuration;

        private readonly string _uSyncTempPath;

        /// <summary>
        /// Create a new uSyncDashboardApi Controller (via DI)
        /// </summary>
        public uSyncDashboardApiController(
            IConfiguration configuration,
            AppCaches appCaches,
            Umbraco.Cms.Core.Hosting.IHostingEnvironment hostingEnvironment,
            IWebHostEnvironment hostEnvironment,
            ILocalizedTextService textService,
            ILogger<uSyncDashboardApiController> logger,
            ITypeFinder typeFinder,
            uSyncService uSyncService,
            SyncHandlerFactory syncHandlerFactory,
            IHubContext<SyncHub> hubContext,
            uSyncConfigService uSyncConfig,
            SyncFileService syncFileService)
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

            _uSyncTempPath = Path.GetFullPath(
                Path.Combine(hostingEnvironment.LocalTempPath, "uSync", "FileImport"));
            _syncFileService = syncFileService;
        }

        /// <summary>
        ///  Stub - get api used to locate api in umbraco
        /// </summary>
        /// <returns></returns>
        public bool GetApi() => true;

        /// <summary>
        ///  Return the uSyncSettings
        /// </summary>
        [HttpGet]
        public uSyncSettings GetSettings()
            => this.uSyncConfig.Settings;

        /// <summary>
        /// Return the default set name based on config
        /// </summary>
        [HttpGet]
        public string GetDefaultSet()
            => this.uSyncConfig.Settings.DefaultSet;

        /// <summary>
        /// Get all the defined sets from the configuration
        /// </summary>
        [HttpGet]
        public IDictionary<string, uSyncHandlerSetSettings> GetSets()
        {
            var section = _configuration.GetSection(uSync.Configuration.uSyncSetsConfig);
            var sets = new Dictionary<string, uSyncHandlerSetSettings>();

            foreach (var item in section.GetChildren())
            {
                sets.Add(item.Key, uSyncConfig.GetSetSettings(item.Key));
            }

            return sets;
        }

        /// <summary>
        ///  gets the sets that can be picked in the dashboard UI. 
        /// </summary>
        public IEnumerable<string> GetSelectableSets()
            => GetSets().Where(x => x.Value.IsSelectable || x.Key.InvariantEquals(uSync.Sets.DefaultSet))
                .Select(x => x.Key);

        /// <summary>
        ///  Return the Handler settings
        /// </summary>
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


        /// <summary>
        ///  zips up your uSync folder and presents it to you. 
        /// </summary>
        [HttpPost]
        public ActionResult DownloadExport()
        {
            var stream = uSyncService.CompressFolder(uSyncConfig.GetRootFolder());
            return new FileStreamResult(stream, MediaTypeNames.Application.Zip)
            {
                FileDownloadName = $"usync_export_{DateTime.Now:yyyMMdd_HHmmss}.zip"
            };
        }

        [HttpPost]
        public async Task<UploadImportResult> UploadImport()
        {
            var file = Request.Form.Files[0];
            var clean = Request.Form["clean"];

            if (file.Length > 0)
            {
                var tempFile = Path.Combine(_uSyncTempPath, 
                    Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".zip");

                Directory.CreateDirectory(Path.GetDirectoryName(tempFile));

                using (var stream = new FileStream(tempFile, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // uSyncService.DeCompressFile(temp, uSyncConfig.GetRootFolder());
                var tempFolder =
                    Path.Combine(_uSyncTempPath, 
                        Path.GetFileNameWithoutExtension(tempFile));

                try
                {
                    //
                    // we could just uncompress direcly into the correct location
                    // but by doing it this way we can run some sanity checks before we wipe 
                    // a users usync folder. 

                    uSyncService.DeCompressFile(tempFile, tempFolder);

                    bool.TryParse(clean, out bool cleanFirst);

                    var errors = _syncFileService.VerifyFolder(tempFolder,
                        uSyncConfig.Settings.DefaultExtension);
                    if (errors.Count > 0)
                    {
                        return new UploadImportResult(false)
                        {
                            Errors = errors
                        };
                    }

                    // copy the files across. 
                    uSyncService.ReplaceFiles(tempFolder,
                        uSyncConfig.GetRootFolder(), cleanFirst);


                    return new UploadImportResult(true);
                }
                catch { throw; }
                finally
                {
                    // remove the temp zip/folder
                    _syncFileService.DeleteFile(tempFile);
                    _syncFileService.DeleteFolder(tempFolder, true);
                }
            }

            throw new ArgumentException("Unsupported");
        }

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class UploadImportResult
        {
            public UploadImportResult(bool success)
            {
                Success = success;
            }

            public bool Success { get; set; }

            public IEnumerable<string> Errors { get; set; }
        }
    }
}
