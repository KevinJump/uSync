using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Http;

using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice.Controllers
{
    /// <summary>
    ///  A simple API controller that you can call to trigger an import 
    /// </summary>
    /// <remarks>
    ///  requires as setting in the web.config that has to exist for the 
    ///  trigger to work. 
    ///  
    /// e.g
    ///   <add key="uSync.TriggerKey" value="FF57D4D6-ABF5-406B-8531-21C51ECC2DD7"/>
    ///
    /// url : {siteurl}/umbraco/usync/uSyncTriggerApi/triggerimport?key=FF57D4D6-ABF5-406B-8531-21C51ECC2DD7
    /// 
    /// If this value is missing, or the guid doesn't match on the URL the trigger will not run. 
    /// 
    /// returns the number of changes that have happend as part of the sync. 
    /// </remarks>
    [PluginController("uSync")]
    public class uSyncTriggerApiController : UmbracoApiController
    {
        private readonly uSyncService _uSyncService;
        private readonly uSyncSettings _uSyncSettings;

        public uSyncTriggerApiController(
            uSyncService uSyncService,
            uSyncConfig uSyncConfig)
        {
            _uSyncService = uSyncService;
            _uSyncSettings = uSyncConfig.Settings;
        }

        [HttpGet]
        public int TriggerImport(Guid key)
        {
            var triggerKey = ConfigurationManager.AppSettings["uSync.TriggerKey"];

            // always return the same thing, give no clues. 

            if (string.IsNullOrWhiteSpace(triggerKey))
                throw new KeyNotFoundException("Missing/Inavlid Key");

            if (!Guid.TryParse(triggerKey, out Guid triggerGuid))
                throw new KeyNotFoundException("Missing/Inavlid Key");

            if (triggerGuid != key)
                throw new KeyNotFoundException("Missing/Inavlid Key");

            var handlerOptions = new SyncHandlerOptions { Group = _uSyncSettings.ImportAtStartupGroup };

            var results = _uSyncService.Import(_uSyncSettings.RootFolder, false, handlerOptions);

            return results.CountChanges();
        }
    }
}
