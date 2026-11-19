namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  A serializable version of an exception, sent in webhook payloads
/// </summary>
public class uSyncExceptionWebhookPayload
{
    /// <summary>
    ///  message of the exception
    /// </summary>
    public string? Message { get; set; }
}
