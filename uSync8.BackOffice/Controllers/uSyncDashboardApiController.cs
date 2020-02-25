using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Composing;
using Umbraco.Core.IO;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Hubs;
using uSync8.BackOffice.Models;
using uSync8.BackOffice.SyncHandlers;

using Constants = Umbraco.Core.Constants;

namespace uSync8.BackOffice.Controllers
{
    [PluginController("uSync")]
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
    public class uSyncDashboardApiController : UmbracoAuthorizedApiController
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

        [HttpGet]
        public JObject GetAddOnSplash()
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(GetContent());
            }
            catch
            {
                // we need to just fail :( 
            }

            return new JObject();
        }

        private string GetContent()
        {
            if (!Config.Settings.AddOnPing) return GetLocal();

            var cachedContent = AppCaches.RuntimeCache.GetCacheItem<string>("usync_addon");
            if (!string.IsNullOrEmpty(cachedContent)) return cachedContent;

            var remote = "https://jumoo.co.uk/usync/addon/82/";
            try
            {
                using (var client = new WebClient())
                {
                    var content = client.DownloadString(remote);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        AppCaches.RuntimeCache.InsertCacheItem<string>("usync_addon", () => content);
                    }

                    return content;
                }
            }
            catch
            {
                return GetLocal();
            }
        }

        private string GetLocal()
        {
            try
            {
                var localFile = IOHelper.MapPath("~/App_Plugins/uSync8/addons.json");
                if (File.Exists(localFile))
                {
                    return File.ReadAllText(localFile);
                }
            }
            catch
            {
            }

            return "{}";
        }

        [HttpGet]
        public AddOnInfo GetAddOns()
        {
            var addOnInfo = new AddOnInfo();

            var addOns = TypeFinder.FindClassesOfType<ISyncAddOn>();
            foreach (var addOn in addOns)
            {
                var instance = Activator.CreateInstance(addOn) as ISyncAddOn;
                if (instance != null)
                {
                    addOnInfo.AddOns.Add(instance);
                }
            }

            addOnInfo.Version = typeof(uSync8.BackOffice.uSync8BackOffice).Assembly.GetName().Version.ToString();

            addOnInfo.AddOns = addOnInfo.AddOns.OrderBy(x => x.SortOrder).ToList();
            addOnInfo.AddOnString = string.Join(", ", addOnInfo.AddOns.Select(x => x.Name));

            return addOnInfo;
        }

        [HttpGet]
        public async Task<uSyncVersionCheck> CheckVersion()
        {
            var cacheInfo = AppCaches.RuntimeCache.GetCacheItem<uSyncVersionCheck>("usync_vcheck");
            if (cacheInfo != null) return cacheInfo;

            var info = await PerformCheck();
            AppCaches.RuntimeCache.InsertCacheItem("usync_vcheck", () => info, new TimeSpan(6, 0, 0));
            return info;
        }

        private async Task<uSyncVersionCheck> PerformCheck()
        {
            // phone home to get the latest version
            var addOnInfo = GetAddOns();

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://jumoo.co.uk");
                    // client.BaseAddress = new Uri("http://jumoo.local");

                    var url = $"/usync/version/?u={addOnInfo.Version}";
                    if (addOnInfo.AddOns.Any())
                    {
                        url += "&a=" + string.Join(":", addOnInfo.AddOns.Select(x => $"{x.Name},{x.Version}"));
                    }

                    var response = await client.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var info = JsonConvert.DeserializeObject<uSyncVersionInfo>(content);

                        var check = new uSyncVersionCheck()
                        {
                            VersionInfo = info,
                            Remote = true,
                        };

                        if (check.VersionInfo.Core.CompareTo(addOnInfo.Version) <= 0)
                        {
                            check.IsCurrent = true;
                        }

                        return check;
                    }
                    else
                    {
                        Logger.Debug<uSyncDashboardApiController>("Failed to get version info, {Content}", content);
                    }
                }
            }
            catch (Exception ex)
            {
                // can't ping.
                Logger.Debug<uSyncDashboardApiController>("Can't ping for version, {Exception}", ex.Message);
            }

            return new uSyncVersionCheck()
            {
                IsCurrent = true,
                VersionInfo = new uSyncVersionInfo()
                {
                    Core = addOnInfo.Version
                }
            };
        }
    }

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncVersionInfo
    {
        public string Core { get; set; }
        public string Complete { get; set; }
        public string Link { get; set; }
        public string Message { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncVersionCheck
    {
        public uSyncVersionInfo VersionInfo { get; set; }
        public bool Remote { get; set; } = false;

        public bool IsCurrent { get; set; }
    }
}
