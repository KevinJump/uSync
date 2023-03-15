using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Controllers;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Extensions;

using uSync.BackOffice.Authorization;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Hubs;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Controllers
{
    [PluginController("uSync")]
    // [Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
    [Authorize(Policy = SyncAuthorizationPolicies.TreeAccessuSync)]
    public partial class uSyncDashboardApiController : UmbracoAuthorizedJsonController
    {

        private readonly AppCaches _appCaches;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<uSyncDashboardApiController> _logger;
        private readonly ILocalizedTextService _textService;

        private readonly uSyncService _uSyncService;
        private readonly SyncFileService _syncFileService;
        private readonly SyncHandlerFactory _handlerFactory;
        private readonly IHubContext<SyncHub> _hubContext;

        private uSyncConfigService _uSyncConfig;

        private readonly ITypeFinder _typeFinder;

        private readonly IConfiguration _configuration;

        private readonly string _uSyncTempPath;

        /// <summary>
        /// Create a new uSyncDashboardApi Controller (via DI)
        /// </summary>
        [ActivatorUtilitiesConstructor]
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
            _appCaches = appCaches;
            _hostEnvironment = hostEnvironment;
            _textService = textService;
            _logger = logger;

            _typeFinder = typeFinder;

            _uSyncService = uSyncService;
            _handlerFactory = syncHandlerFactory;
            _hubContext = hubContext;

            _uSyncConfig = uSyncConfig;

            _configuration = configuration;
            _syncFileService = syncFileService;

            if (hostingEnvironment != null)
            {

                _uSyncTempPath = Path.GetFullPath(
                    Path.Combine(hostingEnvironment.LocalTempPath, "uSync", "FileImport"));
            }
            else
            {
                _uSyncTempPath = Path.GetFullPath(
                    Path.Combine(hostEnvironment.ContentRootPath, "uSync", "Temp", "FileImport"));
            }
        }

        /// <summary>
        ///  Stub - get API used to locate API in umbraco
        /// </summary>
        /// <returns></returns>
        public bool GetApi() => true;

        /// <summary>
        ///  Return the uSyncSettings
        /// </summary>
        [HttpGet]
        public uSyncSettings GetSettings()
            => this._uSyncConfig.Settings;

        /// <summary>
        /// Return the default set name based on configuration
        /// </summary>
        [HttpGet]
        public string GetDefaultSet()
            => this._uSyncConfig.Settings.DefaultSet;

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
                sets.Add(item.Key, _uSyncConfig.GetSetSettings(item.Key));
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
            => this._uSyncConfig.GetSetSettings(id);

        /// <summary>
        ///  get a list of loaded handlers 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<object> GetLoadedHandlers()
            => _handlerFactory.GetAll();

        /// <summary>
        ///  return handler groups for all enabled handlers
        /// </summary>
        [HttpGet]
        public IDictionary<string, string> GetHandlerGroups(string set)
        {
            var handlerSet = !string.IsNullOrWhiteSpace(set) ? set : _uSyncConfig.Settings.DefaultSet;

            var options = new SyncHandlerOptions(handlerSet)
            {
                Group = _uSyncConfig.Settings.UIEnabledGroups
            };

            var groups = _handlerFactory.GetValidHandlerGroupsAndIcons(options);

            // if nothing is set, we add everything marker.
            if (string.IsNullOrWhiteSpace(_uSyncConfig.Settings.UIEnabledGroups) ||
                _uSyncConfig.Settings.UIEnabledGroups.InvariantContains("all"))
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
            return _handlerFactory.GetGroups();
        }

        /// <summary>
        ///  get a list of enabled handlers for the UI
        /// </summary>
        [HttpGet]
        public IEnumerable<SyncHandlerSummary> GetHandlers(string set)
        {
            var handlerSet = !string.IsNullOrWhiteSpace(set) ? set : _uSyncConfig.Settings.DefaultSet;

            var options = new SyncHandlerOptions(handlerSet)
            {
                Group = _uSyncConfig.Settings.UIEnabledGroups,
            };

            return _handlerFactory.GetValidHandlers(options)
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
            var hubClient = new HubClientService(_hubContext, options.ClientId);



            return _uSyncService.Report(_uSyncConfig.GetRootFolder(), new SyncHandlerOptions()
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
            var hubClient = new HubClientService(_hubContext, options.ClientId);

            if (options.Clean)
            {
                _uSyncService.CleanExportFolder(_uSyncConfig.GetRootFolder());
            }

            return _uSyncService.Export(_uSyncConfig.GetRootFolder(), new SyncHandlerOptions()
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
            var hubClient = new HubClientService(_hubContext, options.ClientId);
            return _uSyncService.Import(_uSyncConfig.GetRootFolder(), options.Force, new SyncHandlerOptions()
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
            => _uSyncService.ImportSingleAction(item);

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

            var settings = JsonConvert.SerializeObject(_uSyncConfig.Settings);
            var sets = JsonConvert.SerializeObject(_uSyncConfig.GetSetSettings(uSync.Sets.DefaultSet));

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
        [Authorize(Roles = Constants.Security.AdminGroupAlias)]
        public ActionResult DownloadExport()
        {
            var stream = _uSyncService.CompressFolder(_uSyncConfig.GetRootFolder());
            return new FileStreamResult(stream, MediaTypeNames.Application.Zip)
            {
                FileDownloadName = $"usync_export_{DateTime.Now:yyyMMdd_HHmmss}.zip"
            };
        }

        /// <summary>
        ///  Upload a zip file and import it as the uSync folder
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Constants.Security.AdminGroupAlias)]
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
                    // we could just uncompress directly into the correct location
                    // but by doing it this way we can run some sanity checks before we wipe 
                    // a users usync folder. 

                    _uSyncService.DeCompressFile(tempFile, tempFolder);

                    bool.TryParse(clean, out bool cleanFirst);

                    var errors = _syncFileService.VerifyFolder(tempFolder,
                        _uSyncConfig.Settings.DefaultExtension);
                    if (errors.Count > 0)
                    {
                        return new UploadImportResult(false)
                        {
                            Errors = errors
                        };
                    }

                    // copy the files across. 
                    _uSyncService.ReplaceFiles(tempFolder,
                        _uSyncConfig.GetRootFolder(), cleanFirst);


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

        /// <summary>
        ///  result class - for when an file is uploaded
        /// </summary>
        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        public class UploadImportResult
        {
            /// <summary>
            ///  constructor. 
            /// </summary>
            public UploadImportResult(bool success)
            {
                Success = success;
            }

            /// <summary>
            ///  Upload was success 
            /// </summary>
            public bool Success { get; set; }

            /// <summary>
            ///  Any errors that may have happened as a result of the upload.
            /// </summary>
            public IEnumerable<string> Errors { get; set; }
        }
    }
}
