using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;

using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.SyncHandlers;

namespace uSync.Triggers.Controllers
{
    [PluginController("uSync")]
    public class TriggerController : UmbracoApiController
    {
        private readonly uSyncService _uSyncService;
        private readonly uSyncSettings _uSyncSettings; 

        public TriggerController(uSyncService uSyncService,
            uSyncConfig uSyncConfig)
        {
            _uSyncService = uSyncService;
            _uSyncSettings = uSyncConfig.Settings;
        }

        [HttpGet]
        public string Import(
                Guid Key, 
                string group = "",
                string set = "", 
                bool force = false)
        {
            var triggerKey = ConfigurationManager.AppSettings["uSync.TriggerKey"];

            // always return the same thing, give no clues. 

            if (string.IsNullOrWhiteSpace(triggerKey))
                throw new KeyNotFoundException("Missing/Inavlid Key");

            if (!Guid.TryParse(triggerKey, out Guid triggerGuid))
                throw new KeyNotFoundException("Missing/Inavlid Key");

            if (triggerGuid != Key)
                throw new KeyNotFoundException("Missing/Inavlid Key");

            if (string.IsNullOrWhiteSpace(group))
                group = _uSyncSettings.ImportAtStartupGroup;

            if (string.IsNullOrWhiteSpace(set))
                set = _uSyncSettings.DefaultSet;

            var handlerOptions = new SyncHandlerOptions 
            { 
                Group = group,
                Set = set
            };

            var results = _uSyncService.Import(_uSyncSettings.RootFolder, force, handlerOptions);

            return $"{results.CountChanges()} changes in {results.Count()} items";

        }

    }
}
