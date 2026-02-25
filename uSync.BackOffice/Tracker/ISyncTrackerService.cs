using System;
using System.Threading.Tasks;

namespace uSync.BackOffice.Tracker;

/// <summary>
///  Saves and Retrieves the last import date for the different groups.
/// </summary>
public interface ISyncTrackerService
{
    /// <summary>
    ///  get the last date stored in the db for an import for a given group.
    /// </summary>
    Task<DateTime?> GetLastSync(string group);

    /// <summary>
    ///  set the last import date for a given group
    /// </summary>
    Task SaveLastSync(string group);
}