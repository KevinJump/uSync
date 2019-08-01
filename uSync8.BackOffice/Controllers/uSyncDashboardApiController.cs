using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Web.Http;

using Umbraco.Core.Cache;
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
        private readonly SyncHandlerCollection syncHandlers;

        private uSyncSettings settings;
        private uSyncConfig Config;

        public uSyncDashboardApiController(
            uSyncService uSyncService,
            SyncHandlerCollection syncHandlers,
            uSyncConfig config)
        {
            this.Config = config;
            this.uSyncService = uSyncService;

            this.settings = Current.Configs.uSync();
            this.syncHandlers = syncHandlers;

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
            => syncHandlers.ToList();

        [HttpGet]
        public IEnumerable<string> GetHandlerGroups()
            => syncHandlers.GetValidGroups(settings);

        /*
        [HttpGet]
        public (string version, string addons) GetAddOnString()
        {
            var value = "";
            var addOns = TypeFinder.FindClassesOfType<ISyncAddOn>();
            foreach(var addOn in addOns)
            {
                var instance = Activator.CreateInstance(addOn) as ISyncAddOn;
                if (instance != null)
                {
                    value += $"{instance.Name} "; // [{instance.Version}] ";
                }
            }

            var version = typeof(uSync8.BackOffice.uSync8BackOffice).Assembly.GetName().Version.ToString();

            return (version, value);
        }
        */

        [HttpGet]
        public IEnumerable<object> GetHandlers()
        {
            return syncHandlers.Select(x => new SyncHandlerSummary()
            {
                Icon = x.Icon,
                Name = x.Name,
                Status = HandlerStatus.Pending
            });
        }


        [HttpPost]
        public IEnumerable<uSyncAction> Report(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);
            var summaryClient = new SummaryHandler(hubClient);

            if (string.IsNullOrWhiteSpace(options.Group))
            {
                return uSyncService.Report(settings.RootFolder,
                    new uSyncCallbacks(summaryClient.PostSummary, summaryClient.PostUdate));
            }
            else
            {
                return uSyncService.Report(settings.RootFolder, options.Group,
                    new uSyncCallbacks(summaryClient.PostSummary, summaryClient.PostUdate));
            }
        }

        [HttpPost]
        public IEnumerable<uSyncAction> Export(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);
            var summaryClient = new SummaryHandler(hubClient);

            if (string.IsNullOrWhiteSpace(options.Group))
            {
                return uSyncService.Export(settings.RootFolder,
                    new uSyncCallbacks(summaryClient.PostSummary, summaryClient.PostUdate));
            }
            else
            {
                return uSyncService.Export(settings.RootFolder, options.Group,
                    new uSyncCallbacks(summaryClient.PostSummary, summaryClient.PostUdate));

            }
        }

        [HttpPut]
        public IEnumerable<uSyncAction> Import(uSyncOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);
            var summaryClient = new SummaryHandler(hubClient);

            if (string.IsNullOrWhiteSpace(options.Group))
            {
                return uSyncService.Import(settings.RootFolder, options.Force,
                    new uSyncCallbacks(summaryClient.PostSummary, summaryClient.PostUdate));
            }
            else
            {
                return uSyncService.Import(settings.RootFolder, options.Force, options.Group,
                    new uSyncCallbacks(summaryClient.PostSummary, summaryClient.PostUdate));

            }
        }

        [HttpPut]
        public uSyncAction ImportItem(uSyncAction item)
            => uSyncService.Import(item);

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

            var remote = "https://jumoo.co.uk/usync/addon/";
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

        [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public class AddOnInfo
        {
            public string Version { get; set; }

            public string AddOnString { get; set; }
            public List<ISyncAddOn> AddOns { get; set; } = new List<ISyncAddOn>();
        }

        public class SummaryHandler
        {
            private readonly HubClientService hubClient;

            public SummaryHandler(HubClientService hubClient)
            {
                this.hubClient = hubClient;
            }

            public void PostSummary(SyncProgressSummary summary)
            {
                hubClient.SendMessage(summary);
            }

            public void PostUdate(string message, int count, int total)
            {
                hubClient.SendUpdate(new
                {
                    Message = message,
                    Count = count, 
                    Total = total
                });
            }

        }

        [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
        public class uSyncOptions
        {
            [DataMember(Name = "clientId")]
            public string ClientId { get; set; }

            [DataMember(Name = "force")]
            public bool Force { get; set; }

            [DataMember(Name = "clean")]
            public bool Clean { get; set; }

            [DataMember(Name = "group")]
            public string Group { get; set; }
        }
    }
}
