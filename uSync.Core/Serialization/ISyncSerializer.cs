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

    Task<SyncAttempt<XElement>> SerializeEmptyAsync(TObject item, SyncActionType change, string alias);
    Task<SyncAttempt<XElement>> SerializeAsync(TObject item, SyncSerializerOptions options);
    Task<SyncAttempt<TObject>> DeserializeAsync(XElement node, SyncSerializerOptions options);
    Task<SyncAttempt<TObject>> DeserializeSecondPassAsync(TObject item, XElement node, SyncSerializerOptions options);

    Task<ChangeType> IsCurrentAsync(XElement node, SyncSerializerOptions options);
	Task<ChangeType> IsCurrentAsync(XElement node, XElement? current, SyncSerializerOptions options);

	Task<TObject?> FindItemAsync(XElement node);

	// we are going to not implement this and see what effect it has. 
	// Task<TObject?> FindItemAsync(int id);
	Task<TObject?> FindItemAsync(Guid key);
	Task<TObject?> FindItemAsync(string alias);
	Task SaveAsync(IEnumerable<TObject?> items, Guid userKey);
	Task SaveItemAsync(TObject item, Guid userKey);
	Task DeleteItemAsync(TObject item, Guid userKey);

	/// <summary>
	///  save all the items in umbraco.
	/// </summary>
	/// <param name="items"></param>
	[Obsolete("Use SaveAsync instead, will be removed in v15")]
	void Save(IEnumerable<TObject> items);
	
 //   /// <summary>
	/////  Serialize an empty xml marker, with proposed action
	///// </summary>
	//[Obsolete("Use SerializeEmptyAsync instead, will be removed in v15")]
	//SyncAttempt<XElement> SerializeEmpty(TObject item, SyncActionType change, string alias);

 //   [Obsolete("Use SerializeAsync instead, will be removed in v15")]
 //   SyncAttempt<XElement> Serialize(TObject item, SyncSerializerOptions options);

 //   [Obsolete("Use DeserializeAsync instead, will be removed in v15")]
 //   SyncAttempt<TObject> Deserialize(XElement node, SyncSerializerOptions options);

	//[Obsolete("Use DeserializeSecondPassAsync instead, will be removed in v15")]    
	//SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SyncSerializerOptions options);

	//[Obsolete("Use IsCurrentAsync instead, will be removed in v15")]
	//ChangeType IsCurrent(XElement node, SyncSerializerOptions options);

	//[Obsolete("Use IsCurrentAsync instead, will be removed in v15")]
	//ChangeType IsCurrent(XElement node, XElement? current, SyncSerializerOptions options);

 //   /// <summary>
 //   ///  Find an Item based on the XML node representation
 //   /// </summary>
 //   /// <param name="node"></param>
 //   /// <returns></returns>
 //   [Obsolete("Use FindItemAsync instead, will be removed in v15")] 
 //   TObject? FindItem(XElement node);

 //   /// <summary>
 //   ///  find an item based in its internal id.
 //   /// </summary>
 //   [Obsolete("Use FindItemAsync instead, will be removed in v15")]
 //   TObject? FindItem(int id);

 //   /// <summary>
 //   ///  find an item based on the guid value
 //   /// </summary>
 //   [Obsolete("Use FindItemAsync instead, will be removed in v15")]
 //   TObject? FindItem(Guid key);

 //   /// <summary>
 //   ///  find an item based on the alias
 //   /// </summary>
 //   [Obsolete("Use FindItemAsync instead, will be removed in v15")]
 //   TObject? FindItem(string alias);

 //   /// <summary>
 //   ///  save an item back to umbraco
 //   /// </summary>
 //   /// <param name="item"></param>
 //   [Obsolete("Use SaveItemAsync instead, will be removed in v15")]
 //   void SaveItem(TObject item);

 //   /// <summary>
 //   ///  delete an item from umbraco
 //   /// </summary>
 //   /// <param name="item"></param>
 //   [Obsolete("Use DeleteItemAsync instead, will be removed in v15")]
 //   void DeleteItem(TObject item);

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
