using System;
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
using Umbraco.Core.Composing;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

using uSync8.BackOffice.Models;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core;

namespace uSync8.BackOffice.Controllers
{
    /// <summary>
    ///  Checking status, version numbers and setup 
    /// </summary>
    public partial class uSyncDashboardApiController
    {
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

            addOnInfo.Version = typeof(uSync8.BackOffice.uSync8BackOffice).Assembly.GetName().Version.ToString()
                + uSyncBackOfficeConstants.ReleaseSuffix;

            addOnInfo.AddOns = addOnInfo.AddOns.OrderBy(x => x.SortOrder).ToList();
            addOnInfo.AddOnString = string.Join(", ", 
                    addOnInfo.AddOns
                        .Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name[0] != '_')
                        .Select(x => $"{x.Name} (v{x.Version})"));

            return addOnInfo;
        }

        [HttpGet]
        public async Task<uSyncVersionCheck> CheckVersion()
        {
            var cacheInfo = AppCaches.RuntimeCache.GetCacheItem<uSyncVersionCheck>("usync_vcheck");
            if (cacheInfo != null) return cacheInfo;

            var info = await PerformCheck();
            info.HandlersLoaded = settings.HandlerSets.Count > 0;

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

        /// <summary>
        ///  report any warnings about a sync
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public uSyncWarningMessage GetSyncWarnings(string action, uSyncOptions options)
        {
            var handlers = handlerFactory.GetValidHandlers(new SyncHandlerOptions
            {
                Group = options.Group
            });

            var message = new uSyncWarningMessage();

            if (this.settings.ShowVersionCheckWarning && !uSyncService.CheckVersionFile(this.settings.RootFolder))
            {
                message.Type = "info";
                message.Message = Services.TextService.Localize("usync", "oldformat");
                return message;
            }

            var createOnly = handlers
                .Select(x => x.Handler)
                .Any(h => h.DefaultConfig.GetSetting(uSyncConstants.DefaultSettings.CreateOnly, false));

            if (createOnly)
            {
                message.Type = "warning";
                message.Message = Services.TextService.Localize("usync", "createWarning");
                return message;
            }


            return message;
        }
    }

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class uSyncWarningMessage
    {
        public string Type { get; set; }
        public string Message { get; set; }
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

        public bool HandlersLoaded { get; set; }
    }
}
