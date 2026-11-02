namespace uSync.Backoffice.Management.Api.Models;

/// <summary>
///  the settings of a content or media item that aren't property values.
/// </summary>
public class SyncContentPreviewInfo
{
    /// <summary>
    ///  name of the parent item (null when the item is at the root)
    /// </summary>
    public string? Parent { get; set; }

    public int SortOrder { get; set; }

    public bool Trashed { get; set; }

    /// <summary>
    ///  alias of the template (content only)
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    ///  published state for each culture (an empty culture for invariant content)
    /// </summary>
    public List<SyncContentPreviewCulture> Published { get; set; } = [];
}

public class SyncContentPreviewCulture
{
    public string Culture { get; set; } = string.Empty;

    public bool Published { get; set; }
}
