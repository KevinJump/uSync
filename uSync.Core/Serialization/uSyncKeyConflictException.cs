namespace uSync.Core.Serialization;

/// <summary>
///  thrown when an item on this site matches an item in the uSync files
///  (by name or alias) but has a different key, and Umbraco will not let
///  the key of an existing item change (Umbraco 18+).
/// </summary>
public class uSyncKeyConflictException : InvalidOperationException
{
    public uSyncKeyConflictException(string itemType, string alias, Guid localKey, Guid fileKey, Exception? innerException = null)
        : base(BuildMessage(itemType, alias, localKey, fileKey), innerException)
    {
        ItemType = itemType;
        Alias = alias;
        LocalKey = localKey;
        FileKey = fileKey;
    }

    /// <summary>
    ///  type of the item (e.g. Content, DataType)
    /// </summary>
    public string ItemType { get; }

    /// <summary>
    ///  name or alias of the item.
    /// </summary>
    public string Alias { get; }

    /// <summary>
    ///  the key of the item on this site.
    /// </summary>
    public Guid LocalKey { get; }

    /// <summary>
    ///  the key of the item in the uSync file.
    /// </summary>
    public Guid FileKey { get; }

    public static string BuildMessage(string itemType, string alias, Guid localKey, Guid fileKey)
        => $"Key conflict: '{alias}' ({itemType}) exists on this site with key {localKey}, but the uSync file has key {fileKey}. " +
           $"Umbraco does not allow the key of an existing item to change. See {uSyncConstants.KeyConflictDocsUrl}";
}
