namespace uSync.BackOffice.Models;

/// <summary>
///  a preview and the model a preview mapper built from it.
/// </summary>
public class SyncPreviewResult
{
    /// <summary>
    ///  the preview the handler built.
    /// </summary>
    public required SyncItemPreview Preview { get; set; }

    /// <summary>
    ///  the backoffice entity type of the model, empty when no mapper could map the preview.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    ///  the model the backoffice shows, from the preview mapper.
    /// </summary>
    public object? Model { get; set; }
}
