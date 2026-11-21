using Umbraco.Cms.Core.Models.Entities;

namespace uSync.Core.Serialization;

/// <summary>
///  sets keys on Umbraco entities, turning Umbraco's
///  "Cannot change the Key" error into a <see cref="uSyncKeyConflictException"/>.
/// </summary>
/// <remarks>
///  From Umbraco 18 the key of a saved entity cannot change (umbraco/Umbraco-CMS#21131).
///  Before that the set works, so the same code runs on every version.
/// </remarks>
public static class SyncKeyHelper
{
    private static readonly Lazy<bool> _keysAreImmutable = new(ProbeKeysAreImmutable);

    private static readonly AsyncLocal<ISyncKeyMap?> _mapScope = new();

    /// <summary>
    ///  while the returned scope is open, <see cref="SetKey"/> maps keys on saved
    ///  items instead of changing them (KeyConflict: Map). does nothing when
    ///  the key map isn't enabled.
    /// </summary>
    public static IDisposable BeginKeyMapScope(ISyncKeyMap keyMap)
    {
        if (!keyMap.Enabled) return NullScope.Instance;

        var previous = _mapScope.Value;
        _mapScope.Value = keyMap;
        return new KeyMapScope(previous);
    }

    private sealed class KeyMapScope(ISyncKeyMap? previous) : IDisposable
    {
        public void Dispose() => _mapScope.Value = previous;
    }

    /// <summary>
    ///  true when this version of Umbraco will not let a saved entity's key change.
    /// </summary>
    public static bool KeysAreImmutable => _keysAreImmutable.Value;

    /// <summary>
    ///  set the key of an item, throwing a <see cref="uSyncKeyConflictException"/>
    ///  when Umbraco refuses the change.
    /// </summary>
    public static void SetKey(IEntity item, Guid key, string alias)
    {
        if (item.Key == key) return;

        var localKey = item.Key;

        // key mapping is on: leave the saved item's key alone and map the file key to it.
        if (item.HasIdentity && _mapScope.Value is ISyncKeyMap keyMap)
        {
            keyMap.Add(item.GetType().Name, key, localKey);
            return;
        }

        try
        {
            item.Key = key;
        }
        catch (InvalidOperationException ex) when (item.HasIdentity)
        {
            throw new uSyncKeyConflictException(item.GetType().Name, alias, localKey, key, ex);
        }
    }

    /// <summary>
    ///  true when a found item has a different key to the file, and
    ///  this version of Umbraco will not be able to change it.
    /// </summary>
    public static bool IsKeyConflict(Guid fileKey, Guid localKey)
        => KeysAreImmutable
            && fileKey != Guid.Empty
            && localKey != Guid.Empty
            && fileKey != localKey;

    /// <summary>
    ///  we can't see Umbraco's CanChangeKey, so we try it on a
    ///  throw-away entity that looks saved (has an Id).
    /// </summary>
    private static bool ProbeKeysAreImmutable()
    {
        var probe = new KeyProbeEntity { Id = 1, Key = Guid.NewGuid() };
        try
        {
            probe.Key = Guid.NewGuid();
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private sealed class KeyProbeEntity : EntityBase { }
}
