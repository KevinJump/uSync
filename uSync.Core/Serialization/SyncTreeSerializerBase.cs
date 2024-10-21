using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Serialization;

public abstract class SyncTreeSerializerBase<TObject> : SyncSerializerBase<TObject>
    where TObject : ITreeEntity
{
    protected SyncTreeSerializerBase(IEntityService entityService, ILogger<SyncTreeSerializerBase<TObject>> logger)
        : base(entityService, logger)
    {
    }

    protected abstract Task<Attempt<TObject?>> CreateItemAsync(string alias, ITreeEntity? parent, string itemType);

    #region Getters
    // Getters - get information we already know (either in the object or the XElement)
    protected virtual string GetItemBaseType(XElement node)
        => string.Empty;

    #endregion

    #region Finders 
    // Finders - used on importing, getting things that are already there (or maybe not)


    protected abstract Task<Attempt<TObject?>> FindOrCreateAsync(XElement node);

    protected async Task<TObject?> FindItemAsync(Guid key, string alias)
    {
        var item = await FindItemAsync(key);
        if (item != null) return item;

        return await FindItemAsync(alias);
    }

    #endregion

    /// <summary>
    ///  tree elements need to be parent aware (is the parent missing?)
    /// </summary>
    /// <remarks>
    ///  The change will be marked as a fail if the parent is missing in Umbraco,
    ///  but the handler will also need to confirm the parent isn't in the whole
    ///  import. 
    ///  
    ///  A Serializer has to overwrite HasParentItem, or this will never really
    ///  matter.
    ///  
    ///  A missing parent isn't always a fail, if the settings are such, it might
    ///  be imported into the nearest possible place (e.g one level up), but at 
    ///  report time we say - the parent is missing you know?
    /// </remarks>
    /// 
    public override async Task<ChangeType> IsCurrentAsync(XElement node, SyncSerializerOptions options)
    {
        var change = await base.IsCurrentAsync(node, options);

        // doing this check in isCurrent slows us down a lot, 
        // we also do this check in de-serialize node, so removing it here
        // means reports might not show a missing parent warning but a full
        // import would show an error. 
        //
        // but it is much faster?

        //if (change != ChangeType.NoChange)
        //{
        //    // check parent matches.
        //    if (!HasParentItem(node))
        //    {
        //        return ChangeType.ParentMissing;
        //    }
        //}
        return change;
    }

    /// <summary>
    ///  does the parent item (as defined in the xml) exist in umbraco for this item?
    /// </summary>
    protected virtual Task<bool> HasParentItemAsync(XElement node)
        => Task.FromResult(true);

    /// <summary>
    ///  calculates the Umbraco Path value for an item, based on the parent
    /// </summary>
    protected string CalculateNodePath(TObject item, TObject? parent)
    {
        if (parent == null)
        {
            return string.Join(",", -1, item.Id);
        }
        else
        {
            return string.Join(",", parent.Path, item.Id);
        }
    }

    /// <summary>
    ///  calculates the Level based on the parent.
    /// </summary>
    protected int CalculateNodeLevel(TObject item, TObject? parent)
    {
        if (parent == null)
        {
            return 1;
        }
        else
        {
            return parent.Level + 1;
        }
    }


    [Obsolete("Use CreateItemAsync will be removed in v16")]
    protected virtual Attempt<TObject?> CreateItem(string alias, ITreeEntity? parent, string itemType)
        => CreateItemAsync(alias, parent, itemType).Result;

    [Obsolete("Use FindItemAsync will be removed in v16")]
    protected virtual Attempt<TObject?> FindOrCreate(XElement node)
        => FindOrCreateAsync(node).Result;
    
    [Obsolete("Use FindItemAsync will be removed in v16")]
    protected TObject? FindItem(Guid key, string alias)
        => FindItemAsync(key, alias).Result;
    
    [Obsolete("Use HasParentItemAsync will be removed in v16")]
    protected virtual bool HasParentItem(XElement node)
        => true;

}
