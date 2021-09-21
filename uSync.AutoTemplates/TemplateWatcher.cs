using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace uSync.AutoTemplates
{
    public class TemplateWatcher : IRegisteredObject
    {
        private readonly IApplicationShutdownRegistry _hostingLifetime;
        private readonly FileSystemWatcher _watcher;
        private readonly ILogger<TemplateWatcher> _logger;

        private readonly IFileService _fileService;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly IHostingEnvironment _hostingEnvironment;

        private readonly IFileSystem _templateFileSystem;

        private readonly string _viewsFolder;

        private bool _enabled;
        private bool _deleteMissing;
        private int _delay; 

        public TemplateWatcher(
            IConfiguration configuration,
            IApplicationShutdownRegistry hostingLifetime,
            ILogger<TemplateWatcher> logger,
            IFileService fileService,
            FileSystems fileSystems,
            IShortStringHelper shortStringHelper,
            IHostingEnvironment hostingEnvironment)
        {
            _fileService = fileService;
            _shortStringHelper = shortStringHelper;
            _hostingEnvironment = hostingEnvironment;
            _hostingLifetime = hostingLifetime;

            _logger = logger;

            _templateFileSystem = fileSystems.MvcViewsFileSystem;

            _viewsFolder = _hostingEnvironment.MapPathContentRoot(Constants.SystemDirectories.MvcViews);


            // 
            // use appsettings.json to turn on/off.
            //
            // "uSync" : {
            //      "AutoTemplates" : {
            //          "Enabled": true,
            //          "Delete": false
            //      }
            // }
            //

            _enabled = configuration.GetValue<bool>("uSync:AutoTemplates:Enabled", false); // by default this might be off. 
            _deleteMissing = configuration.GetValue<bool>("uSync:AutoTemplates:Delete", false);
            _delay = configuration.GetValue<int>("uSync:AutoTemplates:Delay", 1000); // wait. 

            _hostingLifetime.RegisterObject(this);
            _watcher = new FileSystemWatcher(_viewsFolder);
            _watcher.Changed += Watcher_FileChanged;
            _watcher.Deleted += Watcher_FileDeleted;
            _watcher.Renamed += Watcher_Renamed;
        }
        public void WatchViewsFolder()
        {
            if (!_enabled) return;
            _watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (!_enabled) return;
            Thread.Sleep(_delay);

            var oldAlias = GetAliasFromFileName(e.OldName);
            var template = _fileService.GetTemplate(oldAlias);
            if (template != null)
            {
                template.Alias = GetAliasFromFileName(e.Name);
                template.Name = Path.GetFileNameWithoutExtension(e.Name);
                _fileService.SaveTemplate(template);
            }
        }

        private void Watcher_FileDeleted(object sender, FileSystemEventArgs e)
        {
            if (_enabled && _deleteMissing)
            {
                Thread.Sleep(_delay);
                var alias = GetAliasFromFileName(e.Name);
                SafeDeleteTemplate(alias);
            }
        }

        private void Watcher_FileChanged(object sender, FileSystemEventArgs e)
        {
            if (!_enabled) return;
            Thread.Sleep(_delay);

            CheckFile(e.Name);
        }

        public void CheckViewsFolder()
        {
            if (!_enabled) return;

            var views = _templateFileSystem.GetFiles(".", "*.cshtml");

            foreach (var view in views)
            {
                CheckFile(view);
            }

            if (_deleteMissing)
            {
                CheckTemplates();
            }
        }

        private void CheckTemplates()
        {
            var templates = _fileService.GetTemplates();
            foreach (var template in templates)
            {
                if (!_templateFileSystem.FileExists(template.Alias + ".cshtml"))
                {
                    SafeDeleteTemplate(template.Alias);
                }
            }
        }


        private static object lockObject = new Object();

        private void CheckFile(string filename)
        {
            try
            {
                if (!_templateFileSystem.FileExists(filename)) return;

                _logger.LogInformation("Checking {filename} template", filename);

                var fileAlias = GetAliasFromFileName(filename);

                // is this from a save inside umbraco ?
                // sometimes there can be a double trigger - 
                if (IsQueued(fileAlias)) return;

                lock (lockObject)
                {

                    var text = GetFileContents(filename);
                    var match = Regex.Match(text, AutoTemplates.LayoutRegEx);

                    if (match == null || match.Groups.Count != 2) return;

                    var layoutFile = match.Groups[1].Value.Trim('"');


                    var fileMasterAlias = GetAliasFromFileName(layoutFile);

                    var template = _fileService.GetTemplate(fileAlias);

                    if (template != null)
                    {
                        var currentMaster = string.IsNullOrWhiteSpace(template.MasterTemplateAlias) ? "null" : template.MasterTemplateAlias;
                        if (fileMasterAlias != currentMaster)
                        {
                            template.SetMasterTemplate(GetMasterTemplate(fileMasterAlias));
                            SafeSaveTemplate(template);
                            return;
                        }
                    }
                    else
                    {
                        // doesn't exist 
                        template = new Template(_shortStringHelper, Path.GetFileNameWithoutExtension(filename), fileAlias);
                        template.SetMasterTemplate(GetMasterTemplate(fileMasterAlias));
                        template.Content = text;

                        SafeSaveTemplate(template);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exeption while checking template file {filename}", filename);
            }
        }

        private ITemplate GetMasterTemplate(string alias)
        {
            if (alias == "null") return null;
            return _fileService.GetTemplate(alias);
        }

        private void SafeDeleteTemplate(string alias)
        {
            try
            {
                _fileService.DeleteTemplate(alias);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exeption while attempting to delete template {alias}", alias);
            }
        }

        private void SafeSaveTemplate(ITemplate template)
        {
            _watcher.EnableRaisingEvents = false;
            _fileService.SaveTemplate(template);
            _watcher.EnableRaisingEvents = true;
        }


        private string GetFileContents(string filename)
        {
            using (Stream stream = _templateFileSystem.OpenFile(filename))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        private string GetAliasFromFileName(string filename)
            => Path.GetFileNameWithoutExtension(filename).ToSafeAlias(_shortStringHelper, false);

        public void Stop(bool immediate)
        {
            _hostingLifetime.UnregisterObject(this);
        }

        ConcurrentDictionary<string, bool> QueuedItems = new ConcurrentDictionary<string, bool>();


        public void QueueChange(string alias)
        {
            QueuedItems.TryAdd(alias.ToLower(), true);
        }

        public bool IsQueued(string alias)
        {
            if (QueuedItems.TryRemove(alias.ToLower(), out bool flag))
                return flag;

            return false;
        }
    }
}
