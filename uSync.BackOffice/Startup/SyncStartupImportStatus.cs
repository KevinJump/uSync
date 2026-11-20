using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace uSync.BackOffice.Startup;

/// <summary>
///  the recorded status of the last import at startup.
/// </summary>
public class SyncStartupImportStatus
{
    /// <summary>
    ///  state of the import.
    /// </summary>
    public SyncStartupImportState State { get; set; } = SyncStartupImportState.None;

    /// <summary>
    ///  when the import started (UTC).
    /// </summary>
    public DateTime? StartedUtc { get; set; }

    /// <summary>
    ///  when the import finished (UTC).
    /// </summary>
    public DateTime? CompletedUtc { get; set; }

    /// <summary>
    ///  number of items processed by the import.
    /// </summary>
    public int ItemsProcessed { get; set; }

    /// <summary>
    ///  number of items that changed.
    /// </summary>
    public int Changes { get; set; }

    /// <summary>
    ///  number of items that had errors.
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    ///  the record says running, but no startup operation is active (the run was abandoned).
    /// </summary>
    /// <remarks>
    ///  worked out when the status is read, it is not stored.
    /// </remarks>
    [JsonIgnore]
    public bool IsStale { get; set; }

    /// <summary>
    ///  build the completed status from the results of a startup import.
    /// </summary>
    internal static SyncStartupImportStatus FromResults(IEnumerable<uSyncAction> results, DateTime startedUtc, DateTime completedUtc)
    {
        var actions = results.ToList();

        return new SyncStartupImportStatus
        {
            State = actions.ContainsErrors()
                ? SyncStartupImportState.CompletedWithErrors
                : SyncStartupImportState.Completed,
            StartedUtc = startedUtc,
            CompletedUtc = completedUtc,
            ItemsProcessed = actions.Count,
            Changes = actions.CountChanges(),
            Errors = actions.CountErrors()
        };
    }
}
