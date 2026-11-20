namespace uSync.Backoffice.Management.Api.Models;

/// <summary>
///  status of the last import at startup - lets a deployment pipeline
///  poll for the end of the startup import.
/// </summary>
public class SyncStartupStatusResponse
{
    /// <summary>
    ///  the startup import state (None, NotConfigured, Blocked, Running, Completed, CompletedWithErrors, Failed)
    /// </summary>
    public required string State { get; set; }

    public DateTime? StartedUtc { get; set; }

    public DateTime? CompletedUtc { get; set; }

    public int ItemsProcessed { get; set; }

    public int Changes { get; set; }

    public int Errors { get; set; }

    /// <summary>
    ///  the state is Running, but no startup operation is active (the run was abandoned).
    /// </summary>
    public bool IsStale { get; set; }
}
