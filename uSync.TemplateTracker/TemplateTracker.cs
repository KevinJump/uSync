using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace uSync.TemplateTracker
{
    public class TemplateTracker
    {
        readonly IFileService fileService;
        readonly string viewFolderPath;
        readonly int rootLength;

        FileSystemWatcher watcher;

        public TemplateTracker(IFileService fileService)
        {
            this.fileService = fileService;
            rootLength = IOHelper.MapPath("~/").Length;
            viewFolderPath = IOHelper.MapPath("~/views");
        }

        public void WatchViewFolder()
        {
            var watcher = new FileSystemWatcher();
                watcher.Path = viewFolderPath;

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            watcher.Filter = "*.cshtml";
            watcher.IncludeSubdirectories = false;

            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            watcher.EnableRaisingEvents = true;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            // rename, have to find the old one and change its view path,

            // then probibly track it like a normal change. 
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var templates = fileService.GetTemplates().ToList();
            UpdateTemplate(e.FullPath, templates);
        }

        public void TrackChanges()
        {
            // var templates = entityService.GetAll(Umbraco.Core.Models.UmbracoObjectTypes.Template);
            // read all teh files in the folder

            var templates = fileService.GetTemplates().ToList();
            foreach (var file in Directory.GetFiles(viewFolderPath, "*.cshtml"))
            {
                UpdateTemplate(file, templates);
            }
        }

        public void UpdateTemplate(string file, IList<ITemplate> templates)
        {
            bool isChange = false;

            // get the layout line - it works out what the parent is called. 
            var virtualPath = file.Substring(rootLength - 1).Replace('\\', '/');
            var template = templates.FirstOrDefault(x => x.VirtualPath.InvariantEquals(virtualPath));
            var content = System.IO.File.ReadAllText(file);

            if (template == null)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                template = new Template(name, name);
            }

            template.Content = content;

            var master = GetMasterFromContent(content, templates);
            if (master != null)
            {
                if (template.MasterTemplateAlias != master.Alias)
                {
                    template.SetMasterTemplate(master);
                    isChange = true;
                }
            }

            if (isChange)
            {
                fileService.SaveTemplate(template);
            }
        }


        private ITemplate GetMasterFromContent(string content, IList<ITemplate> templates)
        {
            var layoutRegEx = "(@{[\\s\\S]*?Layout\\s*?=\\s*?)(\"[^\"]*?\"|null)(;[\\s\\S]*?})";
            var match = Regex.Match(content, layoutRegEx);
            if (match != null)
            {

                var master = match.Groups[2].Value.Trim(new char[] { '"', '\\' });
                var masterPath = Path.Combine(viewFolderPath, master);
                var masterVirtual = masterPath.Substring(rootLength - 1).Replace('\\', '/');
                return templates.FirstOrDefault(x => x.VirtualPath.InvariantEquals(masterVirtual));
            }

            return default;
        }
    }
}
