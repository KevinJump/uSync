using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using uSync.Core.Models;

namespace uSync.Core.Serialization;

public abstract class SyncSerializerRoot<TObject>
{
    protected readonly ILogger<SyncSerializerRoot<TObject>> logger;

    protected readonly Type serializerType;

    protected SyncSerializerRoot(ILogger<SyncSerializerRoot<TObject>> logger)
    {
        this.logger = logger;

        // read the attribute
        serializerType = this.GetType();

        var meta = serializerType.GetCustomAttribute<SyncSerializerAttribute>(false)
            ?? throw new InvalidOperationException($"the uSyncSerializer {serializerType} requires a {typeof(SyncSerializerAttribute)}");

        Name = meta.Name;
        Id = meta.Id;
        ItemType = meta.ItemType;

        IsTwoPass = meta.IsTwoPass;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public string ItemType { get; set; }

    public Type objectType => typeof(TObject);

    public bool IsTwoPass { get; private set; }

    public async Task<SyncAttempt<XElement>> SerializeAsync(TObject item, SyncSerializerOptions options)
        => await this.SerializeCoreAsync(item, options);

    protected abstract Task<SyncAttempt<XElement>> SerializeCoreAsync(TObject item, SyncSerializerOptions options);
    protected abstract Task<SyncAttempt<TObject>> DeserializeCoreAsync(XElement node, SyncSerializerOptions options);

    /// <summary>
    ///  CanDeserialize based on the flags, used to check the model is good, for this import 
    /// </summary>
    protected virtual Task<SyncAttempt<TObject>> CanDeserializeAsync(XElement node, SyncSerializerOptions options)
        => Task.FromResult(SyncAttempt<TObject>.Succeed("No Check", ChangeType.NoChange));

    public async Task<SyncAttempt<TObject>> DeserializeAsync(XElement node, SyncSerializerOptions options)   
    {
        if (node.IsEmptyItem())
        {
            // new behavior when a node is 'empty' that is a marker for a delete or rename
            // so we process that action here, no more action file/folders
            return await ProcessActionAsync(node, options);
        }

        if (!IsValid(node))
            throw new FormatException($"XML Not valid for type {ItemType}");

        if (options.Force || await IsCurrentAsync(node, options) > ChangeType.NoChange)
        {
            // pre-deserialization check. 
            var check = await CanDeserializeAsync(node, options);
            if (!check.Success) return check;

            var alias = node.GetAlias();

            logger.LogDebug(" >> Deserializing {alias} - {type}", alias, ItemType);
            var result = await DeserializeCoreAsync(node, options);
            logger.LogDebug(" << Deserialized result {alias} - {result}", alias, result.Success);

            if (result.Success && result.Item is not null)
            {
                if (!result.Saved && !options.Flags.HasFlag(SerializerFlags.DoNotSave))
                {
                    logger.LogDebug("Saving - {alias}", alias);
                    await SaveItemAsync(result.Item);
                }

                if (options.OnePass)
                {
                    logger.LogDebug("Deserialized {alias} - second pass", alias);
                    return await DeserializeSecondPassAsync(result.Item, node, options);
                }
            }

            return result;
        }

        return SyncAttempt<TObject>.Succeed(node.GetAlias(), ChangeType.NoChange);
    }


    public virtual async Task<SyncAttempt<TObject>> DeserializeSecondPassAsync(TObject item, XElement node, SyncSerializerOptions options)
        => await Task.FromResult(SyncAttempt<TObject>.Succeed(nameof(item), item, typeof(TObject), ChangeType.NoChange));


    /// <summary>
    ///  all xml items now have the same top line, this makes 
    ///  it easier for use to do lookups, get things like
    ///  keys and aliases for the basic checkers etc, 
    ///  makes the code simpler.
    /// </summary>
    /// <param name="item">Item we care about</param>
    /// <param name="alias">Alias we want to use</param>
    /// <param name="level">Level</param>
    /// <returns></returns>
    protected virtual XElement InitializeBaseNode(TObject item, string alias, int level = 0)
        => new XElement(ItemType,
            new XAttribute(uSyncConstants.Xml.Key, ItemKey(item).ToString().ToLower()),
            new XAttribute(uSyncConstants.Xml.Alias, alias),
            new XAttribute(uSyncConstants.Xml.Level, level));

    /// <summary>
    ///  is this a bit of valid xml 
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public virtual bool IsValid(XElement node)
        => node.Name.LocalName == this.ItemType
            && node.GetKey() != Guid.Empty
            && node.GetAlias() != string.Empty;


    /// <summary>
    ///  Is the XML either valid or 'Empty' 
    /// </summary>
    /// <param name="node">XML to examine</param>
    /// <returns>true if file is valid or empty</returns>
    public bool IsValidOrEmpty(XElement node)
        => node.IsEmptyItem() || IsValid(node);

    /// <summary>
    ///  Process the action in teh 'empty' XML node
    /// </summary>
    /// <param name="node">XML to process</param>
    /// <param name="flags">Serializer flags to control options</param>
    /// <returns>Sync attempt detailing changes</returns>

    protected async Task<SyncAttempt<TObject>> ProcessActionAsync(XElement node, SyncSerializerOptions options)
    {
        if (!node.IsEmptyItem())
            throw new ArgumentException("Cannot process actions on a non-empty node");

        var actionType = node.Attribute("Change").ValueOrDefault<SyncActionType>(SyncActionType.None);


        var (key, alias) = FindKeyAndAlias(node);

        logger.LogDebug("Empty Node : Processing Action {actionType} ({key} {alias})", actionType, key, alias);

        switch (actionType)
        {
            case SyncActionType.Delete:
                if (options.DeleteItems())
                    return await ProcessDeleteAsync(key, alias, options.Flags);
                
                return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
            case SyncActionType.Rename:
                return ProcessRename(key, alias, options.Flags);
            case SyncActionType.Clean:
                // we return a 'clean' success, but this is then picked up 
                // in the handler, as something to clean, so the handler does it. 
                return SyncAttempt<TObject>.Succeed(alias, ChangeType.Clean);
            default:
                return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
        }
    }

    protected virtual async Task<SyncAttempt<TObject>> ProcessDeleteAsync(Guid key, string alias, SerializerFlags flags)
    {
        logger.LogDebug("Processing Delete {key} {alias}", key, alias);

        var item = await this.FindItemAsync(key);
        if (item == null && !string.IsNullOrWhiteSpace(alias))
        {
            // we need to build in some awareness of alias matching in the folder
            // because if someone deletes something in one place and creates it 
            // somewhere else the alias will exist, so we don't want to delete 
            // it from over there - this needs to be done at save time 
            // (basically if a create happens) - turn any delete files into renames

            // A Tree Based serializer will return null if you ask it to find 
            // an item solely by alias, so this means we are only deleting by key 
            // on tree's (e.g media, content)
            item = await this.FindItemAsync(alias);
        }

        if (item != null)
        {
            logger.LogDebug("Deleting Item : {alias}", ItemAlias(item));
            await DeleteItemAsync(item);
            return SyncAttempt<TObject>.Succeed(alias, ChangeType.Delete);
        }

        logger.LogDebug("Delete Item not found");
        return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
    }

    protected virtual SyncAttempt<TObject> ProcessRename(Guid key, string alias, SerializerFlags flags)
    {
        logger.LogDebug("Process Rename (no action)");
        return SyncAttempt<TObject>.Succeed(alias, ChangeType.NoChange);
    }

    public virtual async Task<ChangeType> IsCurrentAsync(XElement node, SyncSerializerOptions options)
    {
        XElement? current = default;
        var item = await FindItemAsync(node);
        if (item != null)
        {
            var attempt = await this.SerializeAsync(item, options);
            if (attempt.Success && attempt.Item is not null)
                current = attempt.Item;
        }

        return await IsCurrentAsync(node, current, options);
    }


    public virtual async Task<ChangeType> IsCurrentAsync(XElement node, XElement? current, SyncSerializerOptions options)
    {
        if (node == null) return ChangeType.Update;

        if (!IsValidOrEmpty(node)) throw new FormatException($"Invalid Xml File {node.Name.LocalName}");

        if (current == null)
        {
            if (node.IsEmptyItem())
            {
                // we tell people it's a clean.
                if (node.GetEmptyAction() == SyncActionType.Clean) return ChangeType.Clean;

                // at this point its possible the file is for a rename or delete that has already happened
                return ChangeType.NoChange;
            }
            else
            {
                return ChangeType.Create;
            }
        }

        if (node.IsEmptyItem()) return SyncSerializerRoot<TObject>.CalculateEmptyChange(node, current);

        var newHash = await MakeHashAsync(node);

        var currentHash = await MakeHashAsync(current);
        if (string.IsNullOrEmpty(currentHash)) return ChangeType.Update;

        return currentHash == newHash ? ChangeType.NoChange : ChangeType.Update;
    }

    private static ChangeType CalculateEmptyChange(XElement node, XElement current)
    {
        // this shouldn't happen, but check.
        if (current == null) return ChangeType.NoChange;

        // simple logic, if it's a delete we say so, 
        // renames are picked up by the check on the new file

        return node.GetEmptyAction() switch
        {
            SyncActionType.Delete => ChangeType.Delete,
            SyncActionType.Clean => ChangeType.Clean,
            _ => ChangeType.NoChange,
        };
    }

    public virtual Task<SyncAttempt<XElement>> SerializeEmptyAsync(TObject item, SyncActionType change, string alias)
    {
        logger.LogDebug("Base: Serializing Empty Element {alias} {change}", alias, change);

        if (string.IsNullOrEmpty(alias))
            alias = ItemAlias(item);

        var node = XElementExtensions.MakeEmpty(ItemKey(item), change, alias);

        return Task.FromResult(SyncAttempt<XElement>.Succeed("Empty", node, ChangeType.Removed));
    }


    private async Task<string> MakeHashAsync(XElement node)
    {
        if (node == null) return string.Empty;
        node = CleanseNode(node);
        
        return await node.MakePlatformSafeHashAsync();
	}

	/// <summary>
	///  cleans up the node, removing things that are not generic (like internal Ids)
	///  so that the comparisons are like for like.
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	protected virtual XElement CleanseNode(XElement node) => node;


    #region Finders 
    // Finders - used on importing, getting things that are already there (or maybe not)

    protected (Guid key, string alias) FindKeyAndAlias(XElement node)
    {
        if (IsValidOrEmpty(node))
            return (
                    key: node.Attribute(uSyncConstants.Xml.Key).ValueOrDefault(Guid.Empty),
                    alias: node.Attribute(uSyncConstants.Xml.Alias).ValueOrDefault(string.Empty)
                   );

        return (key: Guid.Empty, alias: string.Empty);
    }

    public abstract Task<TObject?> FindItemAsync(Guid key);
    public abstract Task<TObject?> FindItemAsync(string alias);
    public abstract Task SaveItemAsync(TObject item);
    public abstract Task DeleteItemAsync(TObject item);
    public abstract string ItemAlias(TObject item);
    public abstract Guid ItemKey(TObject item);

    /// <summary>
    ///  for bulk saving, some services do this, it causes less cache hits and 
    ///  so should be faster. 
    /// </summary>
    public virtual async Task SaveAsync(IEnumerable<TObject> items)
    {
        foreach (var item in items)
        {
            await this.SaveItemAsync(item);
        }
    }

    public virtual async Task<TObject?> FindItemAsync(XElement node)
    {
        var (key, alias) = FindKeyAndAlias(node);

        logger.LogTrace("Base: Find Item {key} [{alias}]", key, alias);

        if (key != Guid.Empty)
        {
            var item = await FindItemAsync(key);
            if (item != null) return item;
        }

        if (!string.IsNullOrWhiteSpace(alias))
        {
            logger.LogTrace("Base: Lookup by Alias: {alias}", alias);
            return await FindItemAsync(alias);
        }

        return default;
    }
    #endregion



    //[Obsolete("use SerializeAsync, will be removed in uSync 16")]
    //protected virtual SyncAttempt<XElement> SerializeCore(TObject item, SyncSerializerOptions options)
    //    => SerializeCoreAsync(item, options).Result;
    //[Obsolete("use DeserializeCoreAsync, will be removed in uSync 16")]
    //protected virtual SyncAttempt<TObject> DeserializeCore(XElement node, SyncSerializerOptions options)
    //    => DeserializeCoreAsync(node, options).Result;

    //[Obsolete("use SerializeAsync, will be removed in uSync 16")]
    //public SyncAttempt<XElement> Serialize(TObject item, SyncSerializerOptions options)
    //    => SerializeAsync(item, options).Result;

    //[Obsolete("use DeserializeSecondPassAsync, will be removed in uSync 16")]
    //public virtual SyncAttempt<TObject> DeserializeSecondPass(TObject item, XElement node, SyncSerializerOptions options)
    //    => DeserializeSecondPassAsync(item, node, options).Result;

    //[Obsolete("use DeserializeAsync, will be removed in uSync 16")]
    //public SyncAttempt<TObject> Deserialize(XElement node, SyncSerializerOptions options)
    //    => DeserializeAsync(node, options).Result;
    //[Obsolete("use ProcessActionAsync, will be removed in uSync 16")]
    //protected SyncAttempt<TObject> ProcessAction(XElement node, SyncSerializerOptions options)
    //    => ProcessActionAsync(node, options).Result;

    //[Obsolete("use ProcessDeleteAsync, will be removed in uSync 16")]
    //protected virtual SyncAttempt<TObject> ProcessDelete(Guid key, string alias, SerializerFlags flags)
    //    => ProcessDeleteAsync(key, alias, flags).Result;

    //[Obsolete("use SerializeAsync, will be removed in uSync 16")]
    //public virtual ChangeType IsCurrent(XElement node, SyncSerializerOptions options)
    //    => IsCurrentAsync(node, options).Result;
    //[Obsolete("use SerializeAsync, will be removed in uSync 16")]
    //public virtual ChangeType IsCurrent(XElement node, XElement? current, SyncSerializerOptions options)
    //    => IsCurrentAsync(node, current, options).Result;

    //[Obsolete("use SerializeEmptyAsync, will be removed in uSync 16")]
    //public virtual SyncAttempt<XElement> SerializeEmpty(TObject item, SyncActionType change, string alias)
    //    => SerializeEmptyAsync(item, change, alias).Result;
    //[Obsolete("Finding items by id will be removed in v16")]
    //public abstract TObject? FindItem(int id);

    //[Obsolete("use FindItemAsync, will be removed in uSync 16")]
    //public abstract TObject? FindItem(Guid key);

    //[Obsolete("use FindItemAsync, will be removed in uSync 16")]
    //public abstract TObject? FindItem(string alias);

    //[Obsolete("use SaveItemAsync, will be removed in uSync 16")]
    //public abstract void SaveItem(TObject item);
    //[Obsolete("use DeleteItemAsync, will be removed in uSync 16")]
    //public abstract void DeleteItem(TObject item);
    //[Obsolete("use SaveAsync, will be removed in uSync 16")]
    //public virtual void Save(IEnumerable<TObject> items)
    //    => SaveAsync(items).Wait();

    //[Obsolete("use FindItemAsync, will be removed in uSync 16")]
    //public virtual TObject? FindItem(XElement node)
    //    => FindItemAsync(node).Result;
    //[Obsolete("Use CanDeserializeAsync, will be removed in uSync 16")]
    //protected virtual SyncAttempt<TObject> CanDeserialize(XElement node, SyncSerializerOptions options)
    //=> SyncAttempt<TObject>.Succeed("No Check", ChangeType.NoChange);


}
