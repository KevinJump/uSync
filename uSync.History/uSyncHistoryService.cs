using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Umbraco.Cms.Core.Hosting;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

namespace uSync.History
{
    public class uSyncHistoryService
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly SyncFileService _syncFileService;
        private readonly uSyncConfigService _configService;

        public uSyncHistoryService(
            IHostingEnvironment hostingEnvironment, SyncFileService syncFileService, uSyncConfigService configService)
        {
            _hostingEnvironment = hostingEnvironment;
            _syncFileService = syncFileService;
            _configService = configService;
        }

        public bool IsHistoryEnabled()
        {
            return _configService.Settings.EnableHistory;
        }

        public string GetHistoryFolder()
        {
            var folderSetting = _configService.Settings.HistoryFolder;
            if (string.IsNullOrWhiteSpace(folderSetting))
            {
                var rootFolder = _syncFileService.GetAbsPath(_hostingEnvironment.LocalTempPath);
                var historyFolder = Path.GetFullPath(Path.Combine(rootFolder, "uSync", "history"));
                return historyFolder;
            }
            else { return _syncFileService.GetAbsPath(folderSetting); }
        }

        public bool ClearHistory()
        {
            // 1. get history folder
            string historyFolder = GetHistoryFolder();
            // 2. get history files
            var files = _syncFileService.GetFiles(historyFolder, "*.json");
            // 3. delet this
            foreach (var file in files)
            {
                _syncFileService.DeleteFile(file);
            }
            // 4. truth
            return true;
        }

        public HistoryInfo LoadHistory(string filePath)
        {
            string historyFolder = GetHistoryFolder();
            var fullPath = Path.Combine(historyFolder, filePath);
            string contents = _syncFileService.LoadContent(fullPath);

            var actions = JsonConvert.DeserializeObject<HistoryInfo>(contents);

            actions.FilePath = filePath;

            return actions;
        }

        public IEnumerable<HistoryInfo> GetHistory()
        {
            string historyFolder = GetHistoryFolder();
            var files = _syncFileService.GetFiles(historyFolder, "*.json")
                .Select(x => x.Substring(historyFolder.Length + 1));

            var list = new List<HistoryInfo>();
            foreach (var file in files)
            {
                list.Add(LoadHistory(file));
            }

            return list.OrderByDescending(x => x.Date);
        }

        public void SaveHistoryFile(string historyJson)
        {
            var historyFolder = GetHistoryFolder();
            var historyFile = Path.Combine(historyFolder, DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + ".json");

            _syncFileService.CreateFoldersForFile(historyFile);

            _syncFileService.SaveFile(historyFile, historyJson);
        }
    }
}
