using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Umbraco.Cms.Core.HealthChecks;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core.Serialization;

namespace uSync.BackOffice.HealthChecks;

/// <summary>
///  lists the key map (KeyConflict: Map), and lets you remove or clear mappings.
/// </summary>
[HealthCheck("6B3A5F0E-2C1D-4E8A-9B7F-3D2E1C0A9B8F", "uSync - Key map",
    Description = "Items on this site that match items in the uSync files by name, but have a different key",
    Group = "uSync")]
public class SyncKeyMapHealthCheck : HealthCheck
{
    private const string ClearAlias = "clearKeyMap";
    private const string RemoveAlias = "removeKeyMapEntry";
    private const int MaxListed = 50;

    private readonly ISyncConfigService? _configService;
    private readonly ISyncKeyMap? _keyMap;

    public SyncKeyMapHealthCheck() { }

    /// <summary>
    ///  Constructor
    /// </summary>
    public SyncKeyMapHealthCheck(ISyncConfigService configService, ISyncKeyMap keyMap)
    {
        _configService = configService;
        _keyMap = keyMap;
    }

    /// <inheritdoc/>
    public override Task<IEnumerable<HealthCheckStatus>> GetStatusAsync()
        => Task.FromResult<IEnumerable<HealthCheckStatus>>([GetStatus()]);

    /// <inheritdoc/>
    public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
    {
        if (_keyMap is null) return GetStatus();

        switch (action.Alias)
        {
            case ClearAlias:
                _keyMap.Clear();
                break;
            case RemoveAlias:
                if (Guid.TryParse(action.ProvidedValue, out var fileKey))
                    _keyMap.Remove(fileKey);
                break;
        }

        return GetStatus();
    }

    private HealthCheckStatus GetStatus()
    {
        if (_configService is null || _keyMap is null)
            return new HealthCheckStatus("Key map not available") { ResultType = StatusResultType.Info };

        var entries = _keyMap.GetAll();

        if (_configService.Settings.KeyConflict != SyncKeyConflictMode.Map)
        {
            var off = entries.Count == 0
                ? "Key mapping is off (KeyConflict is Set)."
                : $"Key mapping is off (KeyConflict is Set), but {entries.Count} mappings are stored from when it was on. They are not used.";

            return new HealthCheckStatus(off)
            {
                ResultType = StatusResultType.Info,
                ReadMoreLink = Core.uSyncConstants.KeyConflictDocsUrl,
                Actions = entries.Count > 0 ? [ClearAction()] : []
            };
        }

        if (entries.Count == 0)
        {
            return new HealthCheckStatus("Key mapping is on. No items have been mapped.")
            {
                ResultType = StatusResultType.Success,
                ReadMoreLink = Core.uSyncConstants.KeyConflictDocsUrl
            };
        }

        var lines = entries
            .Take(MaxListed)
            .Select(x => $"{x.EntityType}: {x.FileKey} (file) is {x.LocalKey} (this site)");

        var more = entries.Count > MaxListed ? $"\n...and {entries.Count - MaxListed} more" : string.Empty;

        return new HealthCheckStatus($"{entries.Count} items in the uSync files are mapped to items with a different key on this site.\n{string.Join("\n", lines)}{more}")
        {
            ResultType = StatusResultType.Info,
            ReadMoreLink = Core.uSyncConstants.KeyConflictDocsUrl,
            Actions =
            [
                new HealthCheckAction(RemoveAlias, Id)
                {
                    Name = "Remove a mapping",
                    Description = "Enter the key from the uSync file to remove its mapping (it will be found again on the next import if it still matches by name).",
                    ValueRequired = true,
                    ProvidedValueValidation = "regex",
                    ProvidedValueValidationRegex = "^[0-9a-fA-F]{8}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{4}-?[0-9a-fA-F]{12}$"
                },
                ClearAction()
            ]
        };
    }

    private HealthCheckAction ClearAction()
        => new(ClearAlias, Id)
        {
            Name = "Clear the key map",
            Description = "Remove every mapping. Items that still match by name are mapped again on the next import or report."
        };
}
