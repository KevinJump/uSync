using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Umbraco.Cms.Core.Semver;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Controllers
{
    /// <summary>
    ///  Checking status, version numbers and setup 
    /// </summary>
    public partial class uSyncDashboardApiController
    {
        /// <summary>
        ///  Get the Add-ons splash JSON to display in the addOn tab.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<JObject> GetAddOnSplash()
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(await GetContent());
            }
            catch
            {
                // we need to just fail :( 
            }

            return new JObject();
        }

        private async Task<string> GetContent()
        {
            if (!_uSyncConfig.Settings.AddOnPing) return GetLocal();

            var cachedContent = _appCaches.RuntimeCache.GetCacheItem<string>("usync_json");
            if (!string.IsNullOrEmpty(cachedContent)) return cachedContent;

            var remote = "https://jumoo.co.uk/usync/addon/82/";
            try {
                using (var client = new HttpClient())
                {
                    using(HttpResponseMessage response = await client.GetAsync(remote))
                    {
                        response.EnsureSuccessStatusCode();

                        var json = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            _appCaches.RuntimeCache.InsertCacheItem<string>("usync_json",
                                () => json);
                        }

                        return json;
                    }
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
                var localFile = Path.Combine(_hostEnvironment.ContentRootPath, "App_Plugins", "usync", "addons.txt");
                if (System.IO.File.Exists(localFile))
                {
                    return System.IO.File.ReadAllText(localFile);
                }
            }
            catch
            {
            }

            return "{}";
        }

        /// <summary>
        ///  get the info about installed uSync Add-ons
        /// </summary>
        [HttpGet]
        public AddOnInfo GetAddOns()
        {
            var isAdminUser = _backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser.IsAdmin();

            var addOnInfo = new AddOnInfo();
            var addOns = _typeFinder.FindClassesOfType<ISyncAddOn>();
            foreach (var addOn in addOns)
            {
                var instance = Activator.CreateInstance(addOn) as ISyncAddOn;
                if (instance != null)
                {
                    if (AddOnIsVisible(instance, isAdminUser)) {  
                        addOnInfo.AddOns.Add(instance);
                    }
                }
            }

            addOnInfo.Version = GetuSyncVersion();

            addOnInfo.AddOns = addOnInfo.AddOns.OrderBy(x => x.SortOrder).ToList();
            addOnInfo.AddOnString = string.Join(", ",
                    addOnInfo.AddOns
                        .Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name[0] != '_')
                        .Select(x => $"{x.Name} (v{x.Version})"));

            return addOnInfo;
        }

        /// <summary>
        ///  rules for how an Add on is displayed.
        /// </summary>
        private bool AddOnIsVisible(ISyncAddOn addOn, bool isAdmin)
            => isAdmin || !_uSyncConfig.Settings.HideAddOns.Contains(addOn.Alias, StringComparison.OrdinalIgnoreCase);

        private string GetuSyncVersion()
        {
            var assembly = typeof(uSync).Assembly;
            try
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.GetAssemblyFile().FullName);
                var productVersion = SemVersion.Parse(fileVersionInfo.ProductVersion);
                return productVersion.ToSemanticStringWithoutBuild();
            }
            catch
            {
                return assembly.GetName().Version.ToString(3);
            }
        }

        /// <summary>
        ///  check the uSync version remotely to see if there is a newer recommended version
        /// </summary>
        [HttpGet]
        public async Task<uSyncVersionCheck> CheckVersion()
        {
            var cacheInfo = _appCaches.RuntimeCache.GetCacheItem<uSyncVersionCheck>("usync_vcheck");
            if (cacheInfo != null) return cacheInfo;

            var info = await PerformCheck();
            info.HandlersLoaded = true;

            _appCaches.RuntimeCache.InsertCacheItem("usync_vcheck", () => info, new TimeSpan(6, 0, 0));

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
                        _logger.LogDebug("Failed to get version info, {Content}", content);
                    }
                }
            }
            catch (Exception ex)
            {
                // can't ping.
                _logger.LogDebug("Can't ping for version, {Exception}", ex.Message);
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
        public uSyncWarningMessage GetSyncWarnings([FromQuery]HandlerActions action, uSyncOptions options)
        {
            var handlers = _handlerFactory.GetValidHandlers(new SyncHandlerOptions
            {
                Group = options.Group,
                Action = action
            });

            var message = new uSyncWarningMessage();

            if (this._uSyncConfig.Settings.ShowVersionCheckWarning && !_uSyncService.CheckVersionFile(this._uSyncConfig.GetRootFolder()))
            {
                message.Type = "info";
                message.Message = _textService.Localize("usync", "oldformat");
                return message;
            }

            var createOnly = handlers
                .Where(h => h.Settings.GetSetting(Core.uSyncConstants.DefaultSettings.CreateOnly, false))
                .Select(x => x.Handler.Alias)
                .ToList();

            if (createOnly.Count > 0)
            {
                message.Type = "warning";
                message.Message = _textService.Localize("usync", "createWarning", new [] { string.Join(",", createOnly) });
                return message;
            }


            return message;
        }
    }

    /// <summary>
    ///  uSyncWarning message object 
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncWarningMessage
    {
        /// <summary>
        ///  type of warning (style)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        ///  message to display 
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    ///  uSync version information
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncVersionInfo
    {
        /// <summary>
        ///  core version of uSync
        /// </summary>
        public string Core { get; set; }

        /// <summary>
        ///  version of uSync complete
        /// </summary>
        public string Complete { get; set; }

        /// <summary>
        ///  link to updated version
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        ///  message to show 
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    ///  uSync version check information
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncVersionCheck
    {
        /// <summary>
        ///  version info of current uSync
        /// </summary>
        public uSyncVersionInfo VersionInfo { get; set; }

        /// <summary>
        ///  did we get the version info from a remote source
        /// </summary>
        public bool Remote { get; set; } = false;

        /// <summary>
        ///  is this the 'current' latest version
        /// </summary>
        public bool IsCurrent { get; set; }

        /// <summary>
        ///  have the handlers been loaded (Reserved - always true)
        /// </summary>
        public bool HandlersLoaded { get; set; }
    }
}
