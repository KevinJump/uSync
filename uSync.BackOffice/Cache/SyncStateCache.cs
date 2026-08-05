using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Services;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Extensions;

namespace uSync.BackOffice.Cache;

/// <inheritdoc/>
/// <remarks>
///  <para>
///   the cache is a flat map of "item type + key" to "the hash of the file we last confirmed
///   matched". it lives in the site's temp folder - never in the uSync folder, because it
///   describes this site's database, not the source of truth, and should never travel between
///   environments.
///  </para>
///  <para>
///   the file name and contents both carry an identity - a stamp we keep in Umbraco's key/value
///   table, the uSync version, and a fingerprint of the settings that affect serialization. if
///   any of those change we cannot prove the cache still applies, so we throw it away.
///  </para>
///  <para>
///   invalidations are appended to a small journal file as they happen, and the journal is
///   replayed when we load. without that, an item saved between two uSync runs (or between a
///   run and a restart) could still be sitting in the manifest, and we would wrongly skip it.
///  </para>
/// </remarks>
internal class SyncStateCache : ISyncStateCache
{
    /// <summary>
    ///  key we store our database stamp under, so we can tell if we are looking at the
    ///  same database that wrote the cache.
    /// </summary>
    private const string _databaseStampKey = "uSync.StateCache.Id";

    /// <summary>
    ///  bump this if the shape of the file changes, so old files are discarded.
    /// </summary>
    private const int _formatVersion = 1;

    private const string _cacheFolder = "uSync/cache";

    /// <summary>
    ///  item types whose contents get embedded in *other* items when those are serialized.
    /// </summary>
    /// <remarks>
    ///  a content item's xml carries its doc type alias, template alias and path; a dictionary
    ///  item's carries its languages. so renaming a doc type changes the serialized xml of every
    ///  item that uses it, while leaving those items' own rows (and notifications) untouched.
    ///  there is no cheap way to work out which items are affected, and these changes are rare,
    ///  so we throw the whole cache away instead.
    /// </remarks>
    private static readonly HashSet<string> _sharedItemTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Core.uSyncConstants.Serialization.ContentType,
        Core.uSyncConstants.Serialization.MediaType,
        Core.uSyncConstants.Serialization.MemberType,
        Core.uSyncConstants.Serialization.DataType,
        Core.uSyncConstants.Serialization.Template,
        Core.uSyncConstants.Serialization.Language,
        Core.uSyncConstants.Serialization.ElementContainer,
    };

    private readonly ILogger<SyncStateCache> _logger;
    private readonly ISyncConfigService _configService;
    private readonly ISyncFileService _fileService;
    private readonly IHostingEnvironment _hostingEnvironment;
    private readonly IKeyValueService _keyValueService;

    private readonly ConcurrentDictionary<string, string> _entries = new(StringComparer.Ordinal);

    private readonly SemaphoreSlim _loadLock = new(1, 1);

    /// <summary>
    ///  held while anything changes what we have on disk - invalidations appending to the
    ///  journal, and persist writing the manifest and clearing the journal.
    /// </summary>
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private bool _loaded;
    private bool _usable = true;
    private bool _dirty;
    private bool _manifestOnDisk;

    private string? _identity;
    private string? _manifestFile;
    private string? _journalFile;

    public SyncStateCache(
        ILogger<SyncStateCache> logger,
        ISyncConfigService configService,
        ISyncFileService fileService,
        IHostingEnvironment hostingEnvironment,
        IKeyValueService keyValueService)
    {
        _logger = logger;
        _configService = configService;
        _fileService = fileService;
        _hostingEnvironment = hostingEnvironment;
        _keyValueService = keyValueService;
    }

    /// <inheritdoc/>
    public bool IsEnabled => _configService.Settings.CacheImportState;

    /// <inheritdoc/>
    public int Count => _entries.Count;

    #region Lookup and record

    /// <inheritdoc/>
    public async Task<bool> IsKnownCurrentAsync(XElement node)
    {
        if (IsEnabled is false) return false;

        var entryKey = GetEntryKey(node);
        if (entryKey is null) return false;

        await EnsureLoadedAsync();
        if (_usable is false) return false;

        if (_entries.TryGetValue(entryKey, out var known) is false) return false;

        var hash = await MakeSafeHashAsync(node);
        return hash is not null && string.Equals(hash, known, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public async Task RecordAsync(XElement node)
    {
        if (IsEnabled is false) return;

        var entryKey = GetEntryKey(node);
        if (entryKey is null) return;

        await EnsureLoadedAsync();
        if (_usable is false) return;

        var hash = await MakeSafeHashAsync(node);
        if (hash is null) return;

        _entries[entryKey] = hash;
        _dirty = true;
    }

    /// <summary>
    ///  the key we file an item under - scoped by type, because different types can share a key
    ///  (this is the same scheme <see cref="ISyncFileService.VerifyFolderAsync"/> uses to spot
    ///  clashes).
    /// </summary>
    /// <returns>null if this node is not something we can safely cache</returns>
    private static string? GetEntryKey(XElement? node)
    {
        if (node is null) return null;

        // action files (delete/rename/clean markers) are never cached. whether they are
        // "current" depends on whether the item still exists, and they are filed under the
        // 'Empty' node name, so an invalidation for the real item would never reach them.
        if (node.IsEmptyItem()) return null;

        var key = node.GetKey();
        if (key == Guid.Empty) return null;

        return GetEntryKey(node.Name.LocalName, key);
    }

    private static string GetEntryKey(string itemType, Guid key)
        => $"{itemType}_{key}";

    private async Task<string?> MakeSafeHashAsync(XElement node)
    {
        try
        {
            return await node.MakePlatformSafeHashAsync();
        }
        catch (Exception ex)
        {
            // a cache is never worth failing an import over.
            _logger.LogWarning(ex, "uSync state cache: could not hash a node, treating it as changed");
            return null;
        }
    }

    #endregion

    #region Invalidation

    /// <inheritdoc/>
    public async Task InvalidateAsync(string itemType, Guid key)
    {
        if (string.IsNullOrWhiteSpace(itemType) || key == Guid.Empty) return;

        // see _sharedItemTypes - these change how other items serialize, so a single key
        // is not enough.
        if (_sharedItemTypes.Contains(itemType))
        {
            await InvalidateAllAsync();
            return;
        }

        if (await PrepareForInvalidationAsync() is false) return;

        var entryKey = GetEntryKey(itemType, key);
        await ApplyInvalidationAsync(entryKey, () => _entries.TryRemove(entryKey, out _));
    }

    /// <inheritdoc/>
    public async Task InvalidateTypeAsync(string itemType)
    {
        if (string.IsNullOrWhiteSpace(itemType)) return;

        if (_sharedItemTypes.Contains(itemType))
        {
            await InvalidateAllAsync();
            return;
        }

        if (await PrepareForInvalidationAsync() is false) return;

        await ApplyInvalidationAsync($"#{itemType}", () => RemoveByType(itemType));
    }

    /// <inheritdoc/>
    public async Task InvalidateAllAsync()
    {
        if (await PrepareForInvalidationAsync() is false) return;

        await ApplyInvalidationAsync("*", _entries.Clear);
    }

    /// <summary>
    ///  make sure we are in a position to invalidate anything.
    /// </summary>
    /// <remarks>
    ///  we have to load before we can invalidate. clearing an entry from memory does nothing
    ///  about the copy of it sitting in the file on disk - and the file is what we will trust
    ///  after the next restart. so an item saved before uSync has run even once still has to
    ///  reach the journal.
    ///
    ///  note this happens whether or not the setting is on: if someone turns the cache off,
    ///  edits things, and turns it back on, the file must not still be claiming those items
    ///  are unchanged.
    /// </remarks>
    private async Task<bool> PrepareForInvalidationAsync()
    {
        await EnsureLoadedAsync();
        return _usable;
    }

    private void RemoveByType(string itemType)
    {
        var prefix = $"{itemType}_";
        foreach (var key in _entries.Keys.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            _entries.TryRemove(key, out _);
        }
    }

    /// <summary>
    ///  drop the entries from memory and note the invalidation on disk, so it survives a restart.
    /// </summary>
    /// <remarks>
    ///  writing the whole manifest on every editor save would be far too much, so instead we
    ///  append a line to a journal (a few bytes) and fold it back in the next time we write a
    ///  fresh manifest.
    ///
    ///  both halves happen under the write lock, and <see cref="PersistAsync"/> takes the same
    ///  lock, so an invalidation can never slip in between "manifest written" and "journal
    ///  cleared" and be lost.
    /// </remarks>
    private async Task ApplyInvalidationAsync(string line, Action removeFromMemory)
    {
        await _writeLock.WaitAsync();
        try
        {
            removeFromMemory();

            // so the next persist rewrites the manifest without these entries, and the
            // journal can be cleared rather than growing forever.
            _dirty = true;

            // nothing on disk to correct means nothing to journal - the normal state for
            // anyone who has never turned the cache on.
            if (_journalFile is null || _manifestOnDisk is false) return;

            // deliberately not going through ISyncFileService: it has no append, and this
            // is a few bytes into a temp file that we own.
            await File.AppendAllTextAsync(_journalFile, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // if we cannot journal, we cannot promise the file on disk is right after a
            // restart - so stop trusting it.
            _logger.LogWarning(ex, "uSync state cache: could not record an invalidation, no longer using the cache");
            _usable = false;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private void ApplyJournalLine(string line)
    {
        line = line.Trim();
        if (line.Length == 0) return;

        if (line == "*")
        {
            _entries.Clear();
        }
        else if (line[0] == '#')
        {
            RemoveByType(line[1..]);
        }
        else
        {
            _entries.TryRemove(line, out _);
        }
    }

    #endregion

    #region Load and persist

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        await LoadAsync();
    }

    /// <inheritdoc/>
    public async Task LoadAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            if (_loaded) return;

            // mark as loaded first: whatever happens below, we only try this once, and every
            // failure path leaves us with an empty cache (which just means "check everything").
            _loaded = true;

            _identity = CalculateIdentity();
            if (_identity is null)
            {
                _usable = false;
                return;
            }

            var folder = _fileService.GetAbsPath(Path.Combine(_hostingEnvironment.LocalTempPath, _cacheFolder));
            _manifestFile = Path.Combine(folder, $"state-{_identity}.json");
            _journalFile = Path.Combine(folder, $"state-{_identity}.invalid");

            if (_fileService.FileExists(_manifestFile) is false)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("uSync state cache: no cache file yet, starting empty");
                return;
            }

            _manifestOnDisk = true;

            var content = await _fileService.LoadContentAsync(_manifestFile);
            if (content.TryDeserialize<SyncStateCacheFile>(out var manifest) is false || manifest is null)
            {
                // dirty, so the next run replaces the unreadable file rather than rejecting
                // it again on every restart.
                _dirty = true;
                _logger.LogWarning("uSync state cache: could not read the cache file, starting empty");
                return;
            }

            if (manifest.FormatVersion != _formatVersion ||
                string.Equals(manifest.Identity, _identity, StringComparison.Ordinal) is false)
            {
                // shouldn't happen (the identity is in the file name) but if someone copies a
                // file around, this is what catches it.
                _dirty = true;
                _logger.LogInformation("uSync state cache: cache file no longer applies to this site, starting empty");
                return;
            }

            foreach (var entry in manifest.Entries)
            {
                _entries[entry.Key] = entry.Value;
            }

            await ReplayJournalAsync();

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("uSync state cache: loaded {count} known items", _entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "uSync state cache: failed to load, everything will be checked normally");
            _entries.Clear();
            _usable = false;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task ReplayJournalAsync()
    {
        if (_journalFile is null || _fileService.FileExists(_journalFile) is false) return;

        var journal = await _fileService.LoadContentAsync(_journalFile);
        var lines = journal.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            ApplyJournalLine(line);
        }

        if (lines.Length > 0)
        {
            // anything the journal removed needs to come out of the file too.
            _dirty = true;

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("uSync state cache: replayed {count} invalidations", lines.Length);
        }
    }

    /// <inheritdoc/>
    public async Task PersistAsync()
    {
        // note: we persist even when the setting is off, because a run may have folded
        // invalidations out of the file, and losing those is the one thing that would make
        // the cache lie to us later.
        if (_loaded is false || _usable is false || _dirty is false) return;
        if (_manifestFile is null || _identity is null) return;

        // the whole write happens under the same lock invalidations use, so one cannot land
        // between "manifest written" and "journal cleared" and be thrown away.
        await _writeLock.WaitAsync();
        try
        {
            var manifest = new SyncStateCacheFile
            {
                FormatVersion = _formatVersion,
                Identity = _identity,
                SavedAt = DateTime.UtcNow,
                Entries = _entries.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal),
            };

            _fileService.CreateFoldersForFile(_manifestFile);
            await _fileService.SaveFileAsync(_manifestFile, manifest.SerializeJsonString(false));
            _manifestOnDisk = true;

            // the manifest now includes everything the journal was telling us.
            if (_journalFile is not null && _fileService.FileExists(_journalFile))
                _fileService.DeleteFile(_journalFile);

            _dirty = false;

            CleanUpOldCacheFiles();

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("uSync state cache: saved {count} known items", _entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "uSync state cache: failed to save");
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    ///  remove cache files for identities that no longer apply (e.g. after a settings change).
    /// </summary>
    private void CleanUpOldCacheFiles()
    {
        if (_manifestFile is null) return;

        try
        {
            var folder = Path.GetDirectoryName(_manifestFile);
            if (string.IsNullOrEmpty(folder)) return;

            foreach (var file in _fileService.GetFiles(folder, "state-*.*"))
            {
                if (_fileService.PathMatches(file, _manifestFile)) continue;

                // never our own journal - an invalidation may have re-created it.
                if (_journalFile is not null && _fileService.PathMatches(file, _journalFile)) continue;

                _fileService.DeleteFile(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "uSync state cache: could not tidy up old cache files");
        }
    }

    #endregion

    #region Identity

    /// <summary>
    ///  work out whether the cache we have on disk still describes this site.
    /// </summary>
    /// <returns>null if we cannot tell, in which case the cache is not used at all</returns>
    private string? CalculateIdentity()
    {
        try
        {
            var databaseStamp = GetDatabaseStamp();
            if (databaseStamp is null) return null;

            return HashString($"db={databaseStamp}\n{BuildSettingsFingerprint()}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "uSync state cache: could not work out the cache identity");
            return null;
        }
    }

    /// <summary>
    ///  describe everything that could change what uSync writes out, so that a change to any of
    ///  it means the hashes we recorded stop meaning anything.
    /// </summary>
    /// <remarks>
    ///  built by hand rather than by serializing the settings, because this has to produce the
    ///  same string on every start of the same site. dictionaries bound from configuration
    ///  enumerate in whatever order the configuration provider hands them over, and if that
    ///  wobbled the identity would change on restart and the cache would silently never survive
    ///  one. hence the explicit ordering.
    ///
    ///  erring towards including too much: a setting listed here that turns out not to affect
    ///  the xml just means the cache is discarded more often than it needs to be, which is
    ///  slow. a setting missing from here means the cache could be wrong, which is worse.
    /// </remarks>
    private string BuildSettingsFingerprint()
    {
        var settings = _configService.Settings;
        var set = _configService.GetDefaultSetSettings();

        var parts = new List<string>
        {
            $"version={uSync.Version}",
            $"root={settings.RootFolder}",
            $"folders={string.Join('|', _configService.GetFolders())}",
            $"extension={settings.DefaultExtension}",
            $"foldermode={settings.FolderMode}",
            $"production={settings.ProductionFolder}",
            $"set={settings.DefaultSet}",
            $"disabled={string.Join('|', set.DisabledHandlers.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}",
            $"defaults={Describe(set.HandlerDefaults)}",
        };

        foreach (var handler in set.Handlers.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            parts.Add($"handler:{handler.Key}={Describe(handler.Value)}");
        }

        return string.Join('\n', parts);
    }

    private static string Describe(HandlerSettings? handler)
    {
        if (handler is null) return string.Empty;

        var extra = handler.Settings is null
            ? string.Empty
            : string.Join(',', handler.Settings
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{x.Key}={x.Value}"));

        return string.Join('/', [
            handler.Enabled.ToString(),
            string.Join('+', handler.Actions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
            handler.UseFlatStructure.ToString(),
            handler.GuidNames.ToString(),
            handler.CreateClean.ToString(),
            handler.FullFileOnDifference.ToString(),
            handler.Group,
            $"[{extra}]"
        ]);
    }

    /// <summary>
    ///  a value we keep in the database, so that restoring or pointing at a different database
    ///  invalidates everything we thought we knew.
    /// </summary>
    private string? GetDatabaseStamp()
    {
        try
        {
            var stamp = _keyValueService.GetValue(_databaseStampKey);
            if (string.IsNullOrWhiteSpace(stamp) is false) return stamp;

            stamp = Guid.NewGuid().ToString();
            _keyValueService.SetValue(_databaseStampKey, stamp);
            return stamp;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "uSync state cache: could not read the database stamp");
            return null;
        }
    }

    /// <summary>
    ///  same approach as the xml hashing - we want the same answer on every machine, so
    ///  not GetHashCode.
    /// </summary>
    private static string HashString(string value)
    {
        using HashAlgorithm hashAlgorithm = CryptoConfig.AllowOnlyFipsAlgorithms ? SHA1.Create() : MD5.Create();
        return Convert.ToHexStringLower(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    #endregion

    /// <summary>
    ///  on disk shape of the cache.
    /// </summary>
    internal class SyncStateCacheFile
    {
        public int FormatVersion { get; set; }
        public string Identity { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
        public Dictionary<string, string> Entries { get; set; } = [];
    }
}
