
using Umbraco.Cms.Core;

namespace uSync.Core.Sync;

/// <summary>
///  Representation of a local item, that can be used to kickoff the UI
///  for publishings/exporting.
/// </summary>
public class SyncLocalItem
{
    /// <summary>
    ///  Internal ID for the item
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    ///  Display name for the item
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///  Umbraco UDI value that identifies the item.
    /// </summary>
    // [JsonConverter(typeof(UdiJsonConverter))]
    public Udi Udi { get; set; }

    /// <summary>
    ///  Umbraco/Custom EntityType name
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    ///  details of any language variants
    /// </summary>
    /// <remarks>
    ///  when variants are present the user can be presented with 
    ///  the option of what languages they want to sync.
    /// </remarks>
    [Obsolete("Use All Variants when returning variants so we know what is available.")]
    public Dictionary<string, string> Variants { get; set; }

    /// <summary>
    ///  details of all variants, 
    /// </summary>
    /// <remarks>
    ///  tells us if the variant exists is published or not, allows us to show 
    ///  the unpublished variants in the UI and more importantly tell if they 
    ///  exist. 
    /// </remarks>
    public Dictionary<string, SyncVariantInfo> AllVariants { get; set; }

    /// <summary>
    ///  Syncing of this item requires that the files be synced. 
    ///  e.g if this is a template, we sync the files. because templates
    ///  need files, and they might need the partial views/css/etc.
    /// </summary>
    /// <remarks>
    ///  this value is not yet supported - reserved for future use.
    /// </remarks>
    public bool RequiresFiles { get; set; }


    /// <summary>
    ///  indicates that this item has children
    /// </summary>
    public bool HasChildren { get; set; } = true;

    public SyncLocalItem() { }

    public SyncLocalItem(string id) : this()
    {
        Id = id;
    }

    public SyncLocalItem(int id) : this()
    {
        Id = id.ToString();
    }
}

public class SyncVariantInfo
{
    public string Name { get; set; }
    public bool Published { get; set; }
}
