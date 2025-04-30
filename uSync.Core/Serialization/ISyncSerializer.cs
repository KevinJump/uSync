using System.Xml.Linq;

using uSync.Core.Models;

namespace uSync.Core.Serialization;


public interface ISyncSerializerBase
{
    string Name { get; }

    Type objectType { get; }
}

/// <summary>
///  Generic Serializer ideally we want to load this one into the composition, but 
///  it's type is generic?
/// </summary>
/// <typeparam name="TObject"></typeparam>
public interface ISyncSerializer<TObject> : ISyncSerializerBase
{
    /// <summary>
    ///  Returns true if the piece of xml is valid for this serializer
    /// </summary>
    bool IsValid(XElement node);

    /// <summary>
    ///  this serializer has two passes
    /// </summary>
    bool IsTwoPass { get; }

    /// <summary>
    ///  string representation of the item types used in this serializer
    /// </summary>
    string ItemType { get; }

    /// <summary>
    ///  get the alias we use for any item
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    string ItemAlias(TObject item);

    /// <summary>
    ///  get the key value for the item.
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    abstract Guid ItemKey(TObject item);


    Task SaveAsync(IEnumerable<TObject> items);
    Task<SyncAttempt<XElement>> SerializeEmptyAsync(TObject item, SyncActionType change, string alias);
    Task<SyncAttempt<XElement>> SerializeAsync(TObject item, SyncSerializerOptions options);
    Task<SyncAttempt<TObject>> DeserializeAsync(XElement node, SyncSerializerOptions options);
    Task<SyncAttempt<TObject>> DeserializeSecondPassAsync(TObject item, XElement node, SyncSerializerOptions options);
    Task<ChangeType> IsCurrentAsync(XElement node, SyncSerializerOptions options);
    Task<ChangeType> IsCurrentAsync(XElement node, XElement? current, SyncSerializerOptions options);
    Task<TObject?> FindItemAsync(XElement node);
    Task<TObject?> FindItemAsync(Guid key);
    Task<TObject?> FindItemAsync(string alias);
    Task SaveItemAsync(TObject item);
    Task DeleteItemAsync(TObject item);

}
