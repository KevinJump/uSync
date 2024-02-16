namespace uSync.Core.Sync;

/// <summary>
///  a SyncEntityInfo class provides the information to allow a type to be picked via the UI
/// </summary>
/// <remarks>
///  if the entity can be picked via calling editorService.treepicker then the section alias
///  and tree alias should be all that is required. 
///  
///  if the entity has a custom view for pick (called via editorService.open) then you need
///  to suply the PickerView
/// </remarks>
public class SyncEntityInfo
{
    /// <summary>
    ///  Section entity tree is in within Umbraco
    /// </summary>
    public string SectionAlias { get; set; }

    /// <summary>
    ///  Alias of the tree containing the items
    /// </summary>
    public string TreeAlias { get; set; }

    /// <summary>
    ///  path to the view used for the picker (if not using editorService.treePicker)
    /// </summary>
    public string PickerView { get; set; }

    /// <summary>
    ///  dont allow the user to pick the containers (folders) when using the view 
    /// </summary>
    public bool DoNotPickContainers { get; set; }

}
