namespace uSync.BackOffice.Hubs;

/// <summary>
/// update message sent via uSync to client
/// </summary>
public class uSyncUpdateMessage
{
    /// <summary>
    /// string message to display
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///  number of items processed
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    ///  total number of items we expect to process
    /// </summary>
    public int Total { get; set; }
}
