using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace uSync.BackOffice.Configuration;

/// <summary>
/// Settings for a handler set (group of handlers)
/// </summary>
public class uSyncHandlerSetSettings
{
    /// <summary>
    /// Is this handler set enabled
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// List of groups handlers can belong to.
    /// </summary>
    public string[] HandlerGroups { get; set; } = [];

    /// <summary>
    /// List of disabled handlers
    /// </summary>
    public string[] DisabledHandlers { get; set; } = [];

    /// <summary>
    /// Default settings for all handlers
    /// </summary>
    public HandlerSettings HandlerDefaults { get; set; } = new();

    /// <summary>
    /// Settings for named handlers 
    /// </summary>
    public IDictionary<string, HandlerSettings> Handlers { get; set; }
        = new Dictionary<string, HandlerSettings>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///  for handlers to appear in the drop down on the dashboard they have to be selectable
    /// </summary>
    public bool IsSelectable { get; set; } = false;

    /// <summary>
    ///  Folders this set reads and writes, in place of the global uSync folders.
    /// </summary>
    /// <remarks>
    ///  Leave empty to use the global folders. When set, imports, reports and exports
    ///  for this set only look in these folders, merged in order like the global ones
    ///  (the last folder is the one exports are written to). Folders registered in code are
    ///  still merged in by weight. Only used when FolderMode is Normal.
    ///  A clean export won't empty the export folder if it is, or contains, one of the global folders.
    /// </remarks>
    public string[] Folders { get; set; } = [];
}

/// <summary>
/// Extensions to make handling the settings easier. 
/// </summary>
public static class HandlerSetSettingsExtensions
{
    /// <summary>
    ///  Get the handler settings for the named handler - (will load defaults if no specific handler settings are found)
    /// </summary>
    public static HandlerSettings GetHandlerSettings(this uSyncHandlerSetSettings handlerSet, string alias)
        => handlerSet.Handlers.TryGetValue(alias, out var value)
            ? value.Clone()
            : handlerSet.HandlerDefaults.Clone();
}
