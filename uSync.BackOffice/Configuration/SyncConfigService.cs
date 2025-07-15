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

    private class SyncFolderItem
    {
        public required string Path { get; set; }
        public int Weight { get; set; }
    }

    /// <inheritdoc/>
    public string GetWorkingFolder()
    {
        var folders = FetchFolders();

        return Settings.IsRootSite
            ? folders[0].TrimStart('/')
            : folders.Last().TrimStart('/');
    }

    /// <inheritdoc/>
    public string[] GetFolders()
    {
        var folders = FetchFolders();

        return Settings.IsRootSite
            ? [folders[0].TrimStart('/')]
            : [.. folders.Select(x => x.TrimStart('/'))];
    }

    /// <inheritdoc/>
    public uSyncHandlerSetSettings GetSetSettings(string setName)
        => _setOptionsMonitor.Get(setName);

    /// <inheritdoc/>
    public uSyncHandlerSetSettings GetDefaultSetSettings()
        => GetSetSettings(Settings.DefaultSet);
}
