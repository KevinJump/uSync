namespace uSync.Core;

/// <summary>
///  indicates what happened to an item to cause a ghost file 
///  to be present. 
/// </summary>
public enum SyncActionType
{
    None = 0,
    Rename = 1,
    Delete = 2,
    Clean = 3
}
