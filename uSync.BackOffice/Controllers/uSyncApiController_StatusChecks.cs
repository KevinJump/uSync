﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

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
            if (!uSyncConfig.Settings.AddOnPing) return GetLocal();

            var cachedContent = appCaches.RuntimeCache.GetCacheItem<string>("usync_addon");
            if (!string.IsNullOrEmpty(cachedContent)) return cachedContent;

            var remote = "https://jumoo.co.uk/usync/addon/82/";
            try
            {
                using (var client = new WebClient())
                {
                    var content = client.DownloadString(remote);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        appCaches.RuntimeCache.InsertCacheItem<string>("usync_addon", () => content);
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
                var localFile = Path.Combine(hostEnvironment.ContentRootPath, "App_Plugins", "usync", "addons.txt");
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

        [HttpGet]
        public AddOnInfo GetAddOns()
        {
            var addOnInfo = new AddOnInfo();


            var addOns = typeFinder.FindClassesOfType<ISyncAddOn>();
            foreach (var addOn in addOns)
            {
                var instance = Activator.CreateInstance(addOn) as ISyncAddOn;
                if (instance != null)
                {
                    addOnInfo.AddOns.Add(instance);
                }
            }

            addOnInfo.Version = typeof(global::uSync.BackOffice.uSync).Assembly.GetName().Version.ToString(3)
                + uSyncConstants.ReleaseSuffix;

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
            var cacheInfo = appCaches.RuntimeCache.GetCacheItem<uSyncVersionCheck>("usync_vcheck");
            if (cacheInfo != null) return cacheInfo;

            var info = await PerformCheck();
            info.HandlersLoaded = true;

            appCaches.RuntimeCache.InsertCacheItem("usync_vcheck", () => info, new TimeSpan(6, 0, 0));

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
                        logger.LogDebug("Failed to get version info, {Content}", content);
                    }
                }
            }
            catch (Exception ex)
            {
                // can't ping.
                logger.LogDebug("Can't ping for version, {Exception}", ex.Message);
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
        public uSyncWarningMessage GetSyncWarnings(HandlerActions action, uSyncOptions options)
        {
            var handlers = handlerFactory.GetValidHandlers(new SyncHandlerOptions
            {
                Group = options.Group,
                Action = action
            });

            var message = new uSyncWarningMessage();

            if (this.uSyncConfig.Settings.ShowVersionCheckWarning && !uSyncService.CheckVersionFile(this.uSyncConfig.GetRootFolder()))
            {
                message.Type = "info";
                message.Message = textService.Localize("usync", "oldformat");
                return message;
            }

            var createOnly = handlers
                .Where(h => h.Settings.GetSetting(Core.uSyncConstants.DefaultSettings.CreateOnly, false))
                .Select(x => x.Handler.Alias)
                .ToList();

            if (createOnly.Count > 0)
            {
                message.Type = "warning";
                message.Message = textService.Localize("usync", "createWarning", new [] { string.Join(",", createOnly) });
                return message;
            }


            return message;
        }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncWarningMessage
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncVersionInfo
    {
        public string Core { get; set; }
        public string Complete { get; set; }
        public string Link { get; set; }
        public string Message { get; set; }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class uSyncVersionCheck
    {
        public uSyncVersionInfo VersionInfo { get; set; }
        public bool Remote { get; set; } = false;

        public bool IsCurrent { get; set; }

        public bool HandlersLoaded { get; set; }
    }
}
