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
    ///  Returns true if the peice of xml is valid for this serializer
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
    ///  save all the items in umbraco.
    /// </summary>
    /// <param name="items"></param>

    void Save(IEnumerable<TObject> items);

    /// <summary>
    ///  Serialize an empty xml marker, with proposed action
    /// </summary>
    SyncAttempt<XElement> SerializeEmpty(TObject item, SyncActionType change, string alias);

    SyncAttempt<XElement> Serialize(TObject item, SyncSerializerOptions options);

    SyncAttempt<TObject> Deserialize(XElement node, SyncSerializerOptions options);
    SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SyncSerializerOptions options);

    ChangeType IsCurrent(XElement node, SyncSerializerOptions options);
    ChangeType IsCurrent(XElement node, XElement current, SyncSerializerOptions options);

    /// <summary>
    ///  Find an Item based on the XML node representation
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    TObject? FindItem(XElement node);

    /// <summary>
    ///  find an item based in its internal id.
    /// </summary>
    TObject? FindItem(int id);

    /// <summary>
    ///  find an item based on the guid value
    /// </summary>
    TObject? FindItem(Guid key);

    /// <summary>
    ///  find an item based on the alias
    /// </summary>
    TObject? FindItem(string alias);

    /// <summary>
    ///  save an item back to umbraco
    /// </summary>
    /// <param name="item"></param>
    void SaveItem(TObject item);

    /// <summary>
    ///  delete an item from umbraco
    /// </summary>
    /// <param name="item"></param>
    void DeleteItem(TObject item);

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


}
