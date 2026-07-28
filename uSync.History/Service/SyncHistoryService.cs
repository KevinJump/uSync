using Jumoo.Json;

using System;
using System.Collections.Generic;
using System.Text;

using Umbraco.Cms.Core.Hosting;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;

namespace uSync.History.Service;

internal class SyncHistoryService : ISyncHistoryService
{
    private readonly ISyncFileService _syncFileService;
    private readonly ISyncConfigService _syncConfigService;
    private readonly IHostingEnvironment _hostingEnvironment;

    public SyncHistoryService(ISyncFileService syncFileService, IHostingEnvironment hostingEnvironment, ISyncConfigService syncConfigService)
    {
        _syncFileService = syncFileService;
        _hostingEnvironment = hostingEnvironment;
        _syncConfigService = syncConfigService;
    }

    public bool IsEnabled()
        => _syncConfigService.Settings.EnableHistory;   

    public async Task<IEnumerable<HistoryInfo>> GetHistoryAsync()
    {
        var historyFolder = GetHistoryFolder();
        if (_syncFileService.DirectoryExists(historyFolder) is false) return [];

        var files = _syncFileService.GetFiles(historyFolder, "*.json", true)
            .Select(x => x.Substring(historyFolder.Length + 1));

        var list = new List<HistoryInfo>();

        foreach (var file in files)
        {
            var item = await LoadHistoryAsync(file);
            if (item is not null)
                list.Add(item);

        }

        return list.OrderByDescending(x => x.Date);
    }

    public void ClearHistory()
    {
        string historyFolder = GetHistoryFolder();
        if (_syncFileService.DirectoryExists(historyFolder) is false) return;
        var files = _syncFileService.GetFiles(historyFolder, "*.json", true);
        foreach (var file in files)
        {
            _syncFileService.DeleteFile(file);
        }

    }

    /// <summary>
    ///  returns the full path to the history folder. 
    /// </summary>
    private string GetHistoryFolder()
    {
        var rootFolder = _syncFileService.GetAbsPath(_hostingEnvironment.LocalTempPath);
        var historyFolder = Path.Combine(rootFolder, "uSync", "history");
        return historyFolder;
    }

    private async Task<HistoryInfo?> LoadHistoryAsync(string filePath)
    {
        string historyFolder = GetHistoryFolder();
        var fullPath = Path.Combine(historyFolder, filePath);
        if (!fullPath.StartsWith(historyFolder, StringComparison.OrdinalIgnoreCase))
            return null;

        string contents = await _syncFileService.LoadContentAsync(fullPath);
        var actions = contents.DeserializeJson<HistoryInfo>();
        if (actions is null) return null;

        actions.FilePath = filePath;
        return actions;
    }
}
