using System.Collections.Generic;

namespace uSync.BackOffice.Models;

/// <summary>
/// Information about uSync AddOns (displayed in version string)
/// </summary>
public class AddOnInfo
{
    /// <summary>
    /// Version of uSync we are running
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Display string for all the add ons installed
    /// </summary>
    public string? AddOnString { get; set; }

    /// <summary>
    /// List of all the uSync AddOns installed
    /// </summary>
    public List<ISyncAddOn> AddOns { get; set; } = [];
}
