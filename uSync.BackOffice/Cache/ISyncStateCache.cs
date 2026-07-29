using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace uSync.BackOffice.Cache;

/// <summary>
///  remembers which uSync files we have already confirmed match what is in Umbraco,
///  so repeat imports and reports can skip those items without going near the database.
/// </summary>
/// <remarks>
///  <para>
///   working out if an item has changed normally means loading it from Umbraco, serializing
///   the whole thing, and comparing hashes. that is the bulk of the cost of an import where
///   nothing has actually changed. this cache lets us answer the same question with a single
///   hash of the file we have already loaded.
///  </para>
///  <para>
///   we only ever record a file we have positively confirmed as matching - either the full
///   check ran and said "no change", or we have just written the file out ourselves during an
///   export. we never assume that because an import succeeded the two sides now agree.
///  </para>
///  <para>
///   the cache is a performance aid, not a source of truth. everything about it is designed to
///   fail towards doing the real work: it is off by default, a force import ignores it, and it
///   is discarded wholesale whenever we cannot prove it still applies.
///  </para>
/// </remarks>
public interface ISyncStateCache
{
    /// <summary>
    ///  is the cache turned on (uSync:Settings:CacheImportState)
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    ///  how many items we currently think we know about.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///  have we already confirmed that this exact file content matches what is in Umbraco?
    /// </summary>
    /// <remarks>
    ///  false whenever we are not sure - including when the cache is off, the node is an
    ///  action (delete/rename/clean) marker, or anything at all goes wrong.
    /// </remarks>
    Task<bool> IsKnownCurrentAsync(XElement node);

    /// <summary>
    ///  record that this exact file content is known to match what is in Umbraco.
    /// </summary>
    Task RecordAsync(XElement node);

    /// <summary>
    ///  forget what we knew about a single item.
    /// </summary>
    /// <remarks>
    ///  item types whose contents get embedded in other items (doc types, templates, languages
    ///  and so on) escalate to <see cref="InvalidateAllAsync"/> - renaming a doc type changes
    ///  the serialized xml of every item that uses it, without touching their rows.
    /// </remarks>
    Task InvalidateAsync(string itemType, Guid key);

    /// <summary>
    ///  forget everything we knew about one type of item.
    /// </summary>
    /// <remarks>
    ///  used for moves - moving a node rewrites the path of everything beneath it, so
    ///  invalidating just the moved item is not enough.
    /// </remarks>
    Task InvalidateTypeAsync(string itemType);

    /// <summary>
    ///  forget everything.
    /// </summary>
    Task InvalidateAllAsync();

    /// <summary>
    ///  load the cache from disk (called once, on first use).
    /// </summary>
    Task LoadAsync();

    /// <summary>
    ///  write the cache back to disk, if anything has changed since we loaded it.
    /// </summary>
    Task PersistAsync();
}
