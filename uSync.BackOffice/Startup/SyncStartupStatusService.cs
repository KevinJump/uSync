using Microsoft.Extensions.Logging;

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Services;

namespace uSync.BackOffice.Startup;

internal class SyncStartupStatusService : ISyncStartupStatusService
{
    /// <summary>
    ///  the long running operation type used for the startup import.
    /// </summary>
    internal const string StartupOperationType = "uSync:Startup";

    const string _statusKey = "uSync.StartupImport.Status";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ILogger<SyncStartupStatusService> _logger;
    private readonly IKeyValueService _keyValueService;
    private readonly ILongRunningOperationService _longRunningOperationService;

    public SyncStartupStatusService(
        IKeyValueService keyValueService,
        ILongRunningOperationService longRunningOperationService,
        ILogger<SyncStartupStatusService> logger)
    {
        _keyValueService = keyValueService;
        _longRunningOperationService = longRunningOperationService;
        _logger = logger;
    }

    public Task SetAsync(SyncStartupImportStatus status)
    {
        try
        {
            _keyValueService.SetValue(_statusKey, JsonSerializer.Serialize(status, _jsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving startup import status {State}", status.State);
        }

        return Task.CompletedTask;
    }

    public async Task<SyncStartupImportStatus> GetAsync()
    {
        try
        {
            var value = _keyValueService.GetValue(_statusKey);
            if (string.IsNullOrWhiteSpace(value)) return new SyncStartupImportStatus();

            var status = JsonSerializer.Deserialize<SyncStartupImportStatus>(value, _jsonOptions);
            if (status is null) return new SyncStartupImportStatus();

            if (status.State == SyncStartupImportState.Running)
            {
                var active = await _longRunningOperationService.GetByTypeAsync(StartupOperationType, 0, 1);
                status.IsStale = active.Items.Any() is false;
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving startup import status");
        }

        return new SyncStartupImportStatus();
    }
}
