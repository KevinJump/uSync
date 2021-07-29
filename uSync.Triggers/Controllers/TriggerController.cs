using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Http;

using Umbraco.Core;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;

namespace uSync.Triggers.Controllers
{
    /// <summary>
    ///  Api Trigger for uSync commands - good for batch jobs and CD/CI commands. 
    /// </summary>
    /// <remarks>
    ///  will not work unless you have a TriggerKey setting in web.config 
    ///  
    ///  e.g
    ///   <add key="uSync.TriggerKey" value="FF57D4D6-ABF5-406B-8531-21C51ECC2DD7"/>
    /// 
    ///  url to trigger an import : 
    ///  
    ///  {siteUrl}/umbraco/usync/trigger/import?key=FF57D4D6-ABF5-406B-8531-21C51ECC2DD7
    ///  
    ///  valid actions: 
    ///  
    ///    - import 
    ///    - export 
    /// 
    ///  additonal options : 
    ///  
    ///    group = which group (e.g settings/content)
    ///    set = which handler set to use from config
    ///    folder = where to export/import from 
    ///    
    ///   folder will be limited to uSync folder unless you set uSync.TriggerFolderLimits
    ///   to false in the web.config 
    ///   
    ///  	  <add key="uSync.TriggerFolderLimits" value="false"/>
    ///  
    /// </remarks>
    [PluginController("uSync")]
    public class TriggerController : UmbracoApiController
    {
        private readonly uSyncService _uSyncService;
        private readonly uSyncSettings _uSyncSettings;
        private readonly SyncFileService _fileService;

        public TriggerController(uSyncService uSyncService,
            uSyncConfig uSyncConfig,
            SyncFileService fileService)
        {
            _uSyncService = uSyncService;
            _uSyncSettings = uSyncConfig.Settings;

            _fileService = fileService;
        }

        [HttpGet]
        public string Import(Guid key, string group = "", string set = "", string folder = "", bool force = false)
        {
            EnsureValidKey(key);

            var options = GetOptions(group, set, folder, force);

            var handlerOptions = new SyncHandlerOptions 
            { 
                Group = options.Group,
                Set = options.Set,
            };

            var results = _uSyncService.Import(options.Folder, options.Force, handlerOptions);

            return $"{results.CountChanges()} changes in {results.Count()} items";
        }

        [HttpGet]
        public string Export(Guid key, string group = "", string set = "", string folder = "")
        {
            EnsureValidKey(key);

            var options = GetOptions(group, set, folder);

            var handlerOptions = new SyncHandlerOptions
            {
                Group = options.Group,
                Set = options.Set,
            };

            var result = _uSyncService.Export(options.Folder, handlerOptions);

            return $"{result.CountChanges()} Exported items";
        }

        
        private void EnsureValidKey(Guid key)
        {
            var triggerKey = ConfigurationManager.AppSettings["uSync.TriggerKey"];

            if (string.IsNullOrWhiteSpace(triggerKey))
                throw new KeyNotFoundException("Missing/Inavlid Key");

            if (!Guid.TryParse(triggerKey, out Guid triggerGuid))
                throw new KeyNotFoundException("Missing/Inavlid Key");

            if (triggerGuid != key)
                throw new KeyNotFoundException("Missing/Inavlid Key");
        }

        private void EnsureFolder(string folder)
        {
            var folderLimits = ConfigurationManager.AppSettings["uSync.TriggerFolderLimits"];

            var limits = string.IsNullOrWhiteSpace(folderLimits) || bool.Parse(folderLimits);

            if (limits)
            {
                var absPath = _fileService.GetAbsPath(folder);
                var rootPath = Path.GetFullPath(Path.GetDirectoryName(_uSyncSettings.RootFolder.TrimEnd(Path.DirectorySeparatorChar)));

                if (!absPath.InvariantStartsWith(rootPath))
                    throw new AccessViolationException($"Cannot access {folder} out of uSync folder limits");
            }

            // no folder limits (you can import/export to anywhere on disk!)
          
        }

        private TriggerOptions GetOptions(string group, string set, string folder, bool force = false)
        {
            var options = new TriggerOptions
            {
                Group = string.IsNullOrWhiteSpace(group) ? _uSyncSettings.ImportAtStartupGroup : group,
                Set = string.IsNullOrWhiteSpace(set) ? _uSyncSettings.DefaultSet : set,
                Folder = string.IsNullOrWhiteSpace(folder) ? _uSyncSettings.RootFolder : folder,
                Force = force
            };

            EnsureFolder(options.Folder);

            return options;
        }

    }

    internal class TriggerOptions
    {
        public string Group { get; set; }
        public string Set { get; set; }
        public string Folder { get; set; }
        public bool Force { get; set; }
    }

}
