using System.Xml.Linq;

namespace uSync.Core.Serialization;

/// <summary>
///  a per-site map of keys in the uSync files to the keys of the
///  matching items on this site.
/// </summary>
/// <remarks>
///  From Umbraco 18 the key of a saved item can't change, so when an item
///  is matched by name/alias and has a different key to the file, we map
///  the file key to the local key and swap it in every file we import.
///
///  Only used when KeyConflict is set to Map, otherwise the
///  <see cref="NullSyncKeyMap"/> is used and none of this code runs.
/// </remarks>
public interface ISyncKeyMap
{
    /// <summary>
    ///  is key mapping turned on (KeyConflict: Map).
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    ///  get the local key for a key in the uSync files.
    /// </summary>
    bool TryGetLocal(Guid fileKey, out Guid localKey);

    /// <summary>
    ///  add a mapping from a key in the files to a key on this site.
    /// </summary>
    void Add(string entityType, Guid fileKey, Guid localKey);

    /// <summary>
    ///  remove the mapping for a key in the files.
    /// </summary>
    void Remove(Guid fileKey);

    /// <summary>
    ///  remove any mapping that points at a key on this site (e.g. the item was deleted).
    /// </summary>
    void RemoveLocal(Guid localKey);

    /// <summary>
    ///  remove all mappings.
    /// </summary>
    void Clear();

    /// <summary>
    ///  all the current mappings.
    /// </summary>
    IReadOnlyCollection<SyncKeyMapEntry> GetAll();

    /// <summary>
    ///  return a copy of the node with any mapped keys swapped for the
    ///  local keys (or the node itself, when nothing is mapped).
    /// </summary>
    XElement Localize(XElement node);

    /// <summary>
    ///  hold writes to storage until the returned scope is disposed
    ///  (used while scanning or importing lots of items).
    /// </summary>
    IDisposable BeginBatch();
}

/// <summary>
///  a single key mapping.
/// </summary>
public record SyncKeyMapEntry(Guid FileKey, Guid LocalKey, string EntityType, DateTime Created);

/// <summary>
///  key map used when key mapping is off. does nothing.
/// </summary>
public sealed class NullSyncKeyMap : ISyncKeyMap
{
    public static readonly NullSyncKeyMap Instance = new();

    public bool Enabled => false;

    public bool TryGetLocal(Guid fileKey, out Guid localKey)
    {
        localKey = Guid.Empty;
        return false;
    }

    public void Add(string entityType, Guid fileKey, Guid localKey) { }
    public void Remove(Guid fileKey) { }
    public void RemoveLocal(Guid localKey) { }
    public void Clear() { }
    public IReadOnlyCollection<SyncKeyMapEntry> GetAll() => [];
    public XElement Localize(XElement node) => node;
    public IDisposable BeginBatch() => NullScope.Instance;
}

internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();
    public void Dispose() { }
}
