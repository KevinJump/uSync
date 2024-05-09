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

    [Obsolete("Use CreateItemAsync instead, will be removed in v15")]
    protected virtual Attempt<TObject?> CreateItem(string alias, ITreeEntity? parent, string itemType)
        => CreateItemAsync(alias, parent, itemType).Result;


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
        if (item is not null) return item;

        return await FindItemAsync(alias);

	}

    [Obsolete("Use FindOrCreateAsync instead, will be removed in v15")]
    protected virtual Attempt<TObject?> FindOrCreate(XElement node) => FindOrCreateAsync(node).Result;

    [Obsolete("Use FindItemAsync instead, will be removed in v15")]
	protected TObject? FindItem(Guid key, string alias)
    {
        var item = FindItem(key);
        if (item != null) return item;

        return FindItem(alias);
    }

	#endregion

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
	protected virtual bool HasParentItemAsync(XElement node)
		=> true;

    [Obsolete("Use HasParentItemAsync instead, will be removed in v15")]
	protected virtual bool HasParentItem(XElement node)
        => true;

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


}
