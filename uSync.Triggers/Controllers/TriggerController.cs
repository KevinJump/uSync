using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Http;

using Umbraco.Core;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;

using uSync.Triggers.Auth;

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
    ///  will not work unless you have a uSync.Triggers setting in web.config 
    ///  
    ///  e.g
    ///   <add key="uSync.Triggers" value="True"/>
    /// 
    ///  url to trigger an import : 
    ///  
    ///  {siteUrl}/umbraco/usync/trigger/import
    ///  
    ///  uses basic auth: so needs username/password sent in header (64 bit encoded)
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
    [uSyncTriggerAuth]
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
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
        public object Import(string group = "", string set = "", string folder = "", bool force = false, bool verbose = false)
        {
            var options = GetOptions(group, set, folder, force, verbose);
            return Import(options);
        }

        [HttpPost]
        public object Import(TriggerOptions options)
        {

            EnsureEnabled();

            EnsureOptions(options);

            var sw = Stopwatch.StartNew();

            var handlerOptions = new SyncHandlerOptions 
            { 
                Group = options.Group,
                Set = options.Set,
            };


            var results = _uSyncService.Import(options.Folder, options.Force, handlerOptions);

            sw.Stop();

            if (options.Verbose)
            {
                return results.Where(x => x.Change != uSync8.Core.ChangeType.NoChange);
            }
            else
            {
                return $"{results.CountChanges()} changes in {results.Count()} items in {sw.ElapsedMilliseconds}ms".AsEnumerableOfOne();
            }
        }

        [HttpGet]
        public string Export(string group = "", string set = "", string folder = "", bool force = false, bool verbose = false)
        {
            var options = GetOptions(group, set, folder, force, verbose);
            return Export(options);
        }

        [HttpPost]
        public string Export(TriggerOptions options)
        {
            EnsureEnabled();

            EnsureOptions(options);

            var sw = Stopwatch.StartNew();

            var handlerOptions = new SyncHandlerOptions
            {
                Group = options.Group,
                Set = options.Set,
            };


            var result = _uSyncService.Export(options.Folder, handlerOptions);

            sw.Stop();

            return $"{result.Count()} Exported items in {sw.ElapsedMilliseconds}ms";
        }

        
        private void EnsureEnabled()
        {
            var triggersEnabled = ConfigurationManager.AppSettings["uSync.Triggers"];

            if (string.IsNullOrWhiteSpace(triggersEnabled) || !bool.Parse(triggersEnabled))
                throw new HttpResponseException(System.Net.HttpStatusCode.ServiceUnavailable);
        }

        private void EnsureOptions(TriggerOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Group))
                options.Group = _uSyncSettings.ImportAtStartupGroup;

            if (string.IsNullOrWhiteSpace(options.Set))
                options.Set = _uSyncSettings.DefaultSet;

            if (string.IsNullOrWhiteSpace(options.Folder))
                options.Folder = _uSyncSettings.RootFolder;

            if (options.Group.InvariantEquals("all"))
                options.Group = "";


            var folderLimits = ConfigurationManager.AppSettings["uSync.TriggerFolderLimits"];

            var limits = string.IsNullOrWhiteSpace(folderLimits) || bool.Parse(folderLimits);

            if (limits)
            {
                var absPath = _fileService.GetAbsPath(options.Folder);
                var rootPath = Path.GetFullPath(Path.GetDirectoryName(_uSyncSettings.RootFolder.TrimEnd(Path.DirectorySeparatorChar)));

                if (!absPath.InvariantStartsWith(rootPath))
                    throw new AccessViolationException($"Cannot access {options.Folder} out of uSync folder limits");
            }

            // no folder limits (you can import/export to anywhere on disk!)
          
        }

        private TriggerOptions GetOptions(string group, string set, string folder, bool force = false, bool verbose = false)
        {
            var options = new TriggerOptions
            {
                Group = string.IsNullOrWhiteSpace(group) ? _uSyncSettings.ImportAtStartupGroup : group,
                Set = string.IsNullOrWhiteSpace(set) ? _uSyncSettings.DefaultSet : set,
                Folder = string.IsNullOrWhiteSpace(folder) ? _uSyncSettings.RootFolder : folder,
                Force = force,
                Verbose = verbose
            };

            EnsureOptions(options);

            return options;
        }

    }

    public class TriggerOptions
    {
        public string Group { get; set; }
        public string Set { get; set; }
        public string Folder { get; set; }
        public bool Force { get; set; }

        public bool Verbose { get; set; }
    }

}
