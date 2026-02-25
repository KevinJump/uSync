using Microsoft.Extensions.Logging;

using System;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Services;

namespace uSync.BackOffice.Tracker;


internal class SyncTrackerService : ISyncTrackerService
{
    const string _keyPrefix = "uSync.SyncTracker.";

    private readonly ILogger<SyncTrackerService> _logger;
    private readonly IKeyValueService _keyValueService;

    public SyncTrackerService(IKeyValueService keyValueService, ILogger<SyncTrackerService> logger)
    {
        _keyValueService = keyValueService;
        _logger = logger;
    }

    public Task SaveLastSync(string group)
    {
        try
        {
            var key = $"{_keyPrefix}{group}";
            _keyValueService.SetValue(key, DateTime.UtcNow.ToString("o"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving last sync time for group {Group}", group);
        }
        return Task.CompletedTask;
       
    }

    public async Task<DateTime?> GetLastSync(string group)
    {
        var lastSync = await InternalGetLastSync(group);
        var lastAllSync = await InternalGetLastSync("All");
        return lastSync > lastAllSync ? lastSync : lastAllSync;
    }

    private Task<DateTime?> InternalGetLastSync(string group)
    {
        try
        {
            var key = $"{_keyPrefix}{group}";
            var value = _keyValueService.GetValue(key);
            if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
            {
                return Task.FromResult<DateTime?>(result);
            }
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving last sync time for group {Group}", group);
        } 

        return Task.FromResult<DateTime?>(null);
    }
}
