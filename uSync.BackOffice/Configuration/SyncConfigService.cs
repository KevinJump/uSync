using Microsoft.Extensions.Options;

using System.Collections.Generic;
using System.Linq;

using Umbraco.Extensions;

namespace uSync.BackOffice.Configuration;

/// <inheritdoc/>
internal class SyncConfigService : ISyncConfigService
{
    private readonly IOptionsMonitor<uSyncHandlerSetSettings> _setOptionsMonitor;
    private readonly SyncFolderCollection _syncFolders;

    /// <inheritdoc/>
    public uSyncSettings Settings { get; private set; }

    /// <summary>
    /// Constructor for config service
    /// </summary>
    public SyncConfigService(
        IOptionsMonitor<uSyncSettings> settingsOptionsMonitor,
        IOptionsMonitor<uSyncHandlerSetSettings> setOptionsMonitor,
        SyncFolderCollection syncFolders)
    {
        Settings = settingsOptionsMonitor.CurrentValue;

        settingsOptionsMonitor.OnChange(options =>
        {
            Settings = options;
        });

        _setOptionsMonitor = setOptionsMonitor;
        _syncFolders = syncFolders;
    }

    private string[] FetchFolders()
    {
        var folders = Settings.Folders
            .Select((x, index) => new SyncFolderItem
            {
                Path = x.TrimStart('/').EnsureEndsWith('/'),
                Weight = index * 1000
            })
            .ToList();

        folders.AddRange(
        _syncFolders.Select(x => new SyncFolderItem
        {
            Path = x.Path.TrimStart('/').EnsureEndsWith('/'),
            Weight = x.Weight
        }));

        return folders.OrderBy(x => x.Weight)
            .Select(x => x.Path)
            .ToArray();
    }

    private sealed class SyncFolderItem
    {
        public required string Path { get; set; }
        public int Weight { get; set; }
    }

    /// <inheritdoc/>
    public string GetWorkingFolder()
    {
        var folders = GetFolders();
        return folders.Last().TrimStart('/');
    }

    /// <inheritdoc/>
    public string[] GetFolders()
    {
        var folders = FetchFolders();

        switch(Settings.FolderMode)
        {
            case SyncFolderMode.Root:
                return [folders[0].TrimStart('/')];
            case SyncFolderMode.Production:
                return [Settings.ProductionFolder.TrimStart('/')];
            default:
                return [.. folders.Select(x => x.TrimStart('/'))];
        }
    }

    /// <inheritdoc/>
    public uSyncHandlerSetSettings GetSetSettings(string setName)
        => _setOptionsMonitor.Get(setName) ?? new uSyncHandlerSetSettings();

    /// <inheritdoc/>
    public uSyncHandlerSetSettings GetDefaultSetSettings()
        => GetSetSettings(Settings.DefaultSet);
}
