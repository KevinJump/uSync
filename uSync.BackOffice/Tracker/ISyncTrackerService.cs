using System;
using System.Threading.Tasks;

namespace uSync.BackOffice.Tracker;

public interface ISyncTrackerService
{
    Task<DateTime?> GetLastSync(string group);
    Task SaveLastSync(string group);
}