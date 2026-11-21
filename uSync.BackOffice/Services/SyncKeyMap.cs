using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Services;

using uSync.BackOffice.Configuration;
using uSync.Core.Serialization;

namespace uSync.BackOffice.Services;

/// <summary>
///  key map stored as a single value in Umbraco's key/value table.
/// </summary>
/// <remarks>
///  The map is expected to stay small (the items that were created separately
///  on this site), so it's loaded whole into memory on first use and written
///  back whole when it changes. Writes are held while a batch is open.
/// </remarks>
internal sealed class SyncKeyMap : ISyncKeyMap
{
    internal const string StorageKey = "uSync_KeyMap";

    private readonly IKeyValueService _keyValueService;
    private readonly ISyncConfigService _configService;
    private readonly ILogger<SyncKeyMap> _logger;

    private readonly Lock _lock = new();

    // replaced (not changed) on every write, so readers never need the lock.
    private volatile Dictionary<Guid, SyncKeyMapEntry>? _entries;
    private volatile Dictionary<Guid, Guid> _lookup = [];

    private int _batchDepth;
    private bool _dirty;

    public SyncKeyMap(IKeyValueService keyValueService, ISyncConfigService configService, ILogger<SyncKeyMap> logger)
    {
        _keyValueService = keyValueService;
        _configService = configService;
        _logger = logger;
    }

    public bool Enabled => _configService.Settings.KeyConflict == SyncKeyConflictMode.Map;

    public bool TryGetLocal(Guid fileKey, out Guid localKey)
    {
        EnsureLoaded();
        return _lookup.TryGetValue(fileKey, out localKey);
    }

    public void Add(string entityType, Guid fileKey, Guid localKey)
    {
        if (!Enabled || fileKey == Guid.Empty || localKey == Guid.Empty || fileKey == localKey) return;

        Update(entries =>
        {
            if (entries.TryGetValue(fileKey, out var existing) && existing.LocalKey == localKey)
                return false;

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Key map: {entityType} {fileKey} in the uSync files is {localKey} on this site", entityType, fileKey, localKey);

            entries[fileKey] = new SyncKeyMapEntry(fileKey, localKey, entityType, DateTime.UtcNow);
            return true;
        });
    }

    public void Remove(Guid fileKey)
        => Update(entries => entries.Remove(fileKey));

    public void RemoveLocal(Guid localKey)
        => Update(entries =>
        {
            var keys = entries.Values.Where(x => x.LocalKey == localKey).Select(x => x.FileKey).ToList();
            foreach (var key in keys) entries.Remove(key);
            return keys.Count > 0;
        });

    public void Clear()
        => Update(entries =>
        {
            var any = entries.Count > 0;
            entries.Clear();
            return any;
        });

    public IReadOnlyCollection<SyncKeyMapEntry> GetAll()
    {
        EnsureLoaded();
        return [.. _entries!.Values.OrderBy(x => x.EntityType).ThenBy(x => x.Created)];
    }

    public XElement Localize(XElement node)
    {
        if (!Enabled) return node;
        EnsureLoaded();
        return SyncKeyLocalizer.Localize(node, _lookup);
    }

    public IDisposable BeginBatch()
    {
        if (!Enabled) return NullSyncKeyMap.Instance.BeginBatch();

        lock (_lock) _batchDepth++;
        return new Batch(this);
    }

    private void EndBatch()
    {
        lock (_lock)
        {
            _batchDepth--;
            if (_batchDepth == 0 && _dirty) Save();
        }
    }

    private void Update(Func<Dictionary<Guid, SyncKeyMapEntry>, bool> change)
    {
        lock (_lock)
        {
            EnsureLoaded();

            var entries = new Dictionary<Guid, SyncKeyMapEntry>(_entries!);
            if (!change(entries)) return;

            _entries = entries;
            _lookup = entries.ToDictionary(x => x.Key, x => x.Value.LocalKey);
            _dirty = true;

            if (_batchDepth == 0) Save();
        }
    }

    private void EnsureLoaded()
    {
        if (_entries is not null) return;

        lock (_lock)
        {
            if (_entries is not null) return;

            var entries = Load();
            _lookup = entries.ToDictionary(x => x.Key, x => x.Value.LocalKey);
            _entries = entries;
        }
    }

    private Dictionary<Guid, SyncKeyMapEntry> Load()
    {
        try
        {
            var json = _keyValueService.GetValue(StorageKey);
            if (string.IsNullOrWhiteSpace(json)) return [];

            var items = JsonSerializer.Deserialize<List<SyncKeyMapEntry>>(json) ?? [];
            return items.ToDictionary(x => x.FileKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load the uSync key map, starting with an empty map");
            return [];
        }
    }

    private void Save()
    {
        _keyValueService.SetValue(StorageKey, JsonSerializer.Serialize(_entries!.Values.ToList()));
        _dirty = false;
    }

    private sealed class Batch(SyncKeyMap map) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                map.EndBatch();
        }
    }
}
