using System;
using System.Collections.Generic;
using System.ComponentModel;

using Umbraco.Extensions;

using uSync.Core.Extensions;

namespace uSync.BackOffice.Configuration;

/// <summary>
/// Settings to control who a handler works
/// </summary>
public class HandlerSettings
{
    /// <summary>
    /// Is handler enabled or disabled
    /// </summary>
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// List of actions the handler is configured for. 
    /// </summary>
    public string[] Actions { get; set; } = [];

    /// <summary>
    /// Should use a flat folder structure when exporting items
    /// </summary>
    [DefaultValue(true)]
    public bool UseFlatStructure { get; set; } = true;

    /// <summary>
    /// Items should be saved with their guid/key value as the filename
    /// </summary>
    [DefaultValue(false)]
    public bool GuidNames { get; set; } = false;

    /// <summary>
    /// Imports should fail if the parent item is missing (if false, item be imported go a close as possible to location)
    /// </summary>
    [DefaultValue(false)]
    public bool FailOnMissingParent { get; set; } = false;

    /// <summary>
    /// Override the group the handler belongs too.
    /// </summary>
    [DefaultValue("")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    ///  create a corresponding _clean file for this export 
    /// </summary>
    /// <remarks>
    ///  the clean file will only get created if the item in question has children.
    /// </remarks>
    public bool CreateClean { get; set; } = false;

    /// <summary>
    ///  when saving root items with differences, save all the items, (this is the legacy behavior)
    /// </summary>
    /// <remarks>
    ///  pre v13.3 when a change is made the whole .config file is saved to the new ./usync folder
    ///  but this is a bug, the intention was and it that only the changes are saved to the folder
    ///  this turns the old behavior back on. 
    /// </remarks>
    public bool FullFileOnDifference { get; set; } = false;

    /// <summary>
    /// Additional settings for the handler
    /// </summary>

    // TODO: v13 - change this to string, object settings collection. 
    //             makes for better intellisense from schema.
    public Dictionary<string, object?> Settings { get; set; }
        = new Dictionary<string, object?>(StringComparer.InvariantCultureIgnoreCase);
}

/// <summary>
///  Extensions to the handler settings
/// </summary>
public static class HandlerSettingsExtensions
{
    /// <summary>
    ///  get a setting from the settings dictionary.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="settings"></param>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static TResult GetSetting<TResult>(this HandlerSettings settings, string key, TResult defaultValue)
    {
        if (settings.Settings != null && settings.Settings.TryGetValue(key, out var value) && value is not null)
        {
            if (value.TryGetValueAs<TResult>(out var result) && result is not null)
                return result;
        }

        return defaultValue;
    }

    /// <summary>
    ///  Add a setting to the settings Dictionary (creating the dictionary if its missing)
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void AddSetting<TObject>(this HandlerSettings settings, string key, TObject value)
    {
        settings.Settings ??= new Dictionary<string, object?>(StringComparer.InvariantCultureIgnoreCase);

        settings.Settings.TryAdd(key, value?.ToString() ?? string.Empty);
    }

    /// <summary>
    ///  create a copy of this handlers settings
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    public static HandlerSettings Clone(this HandlerSettings settings)
    {
        return new HandlerSettings
        {
            Actions = settings.Actions,
            Enabled = settings.Enabled,
            FailOnMissingParent = settings.FailOnMissingParent,
            UseFlatStructure = settings.UseFlatStructure,
            Group = settings.Group,
            GuidNames = settings.GuidNames,
            CreateClean = settings.CreateClean,
            FullFileOnDifference = settings.FullFileOnDifference,
            Settings = settings.Settings is not null
                ? new Dictionary<string, object?>(settings.Settings, StringComparer.InvariantCultureIgnoreCase)
                : new Dictionary<string, object?>(StringComparer.InvariantCultureIgnoreCase)
        };
    }

    /// <summary>
    ///  Merge a handler's own settings over the top of a set of default settings.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Returns a new <see cref="HandlerSettings"/> so neither input is mutated.
    ///  </para>
    ///  <para>
    ///   The strongly typed properties (Enabled, UseFlatStructure, GuidNames, etc.) are taken from
    ///   <paramref name="handlerSettings"/>. Configuration binding cannot tell an unset boolean from
    ///   one explicitly set to its default value, so we can't reliably layer these over the defaults
    ///   without risking overriding a deliberately-set value - the handler's own block wins for them.
    ///  </para>
    ///  <para>
    ///   The additional <see cref="HandlerSettings.Settings"/> dictionary <i>is</i> merged, because a
    ///   key is only present when it has been explicitly configured. The <paramref name="defaults"/>
    ///   provide the base and the handler's own keys take precedence - so a per-key default (e.g.
    ///   CreateOnly) set in HandlerDefaults now cascades to handlers that define their own block.
    ///  </para>
    /// </remarks>
    public static HandlerSettings MergeWithDefaults(this HandlerSettings handlerSettings, HandlerSettings defaults)
    {
        var merged = handlerSettings.Clone();

        // start from the defaults, then layer the handler's own keys on top.
        var settings = defaults.Settings is not null
            ? new Dictionary<string, object?>(defaults.Settings, StringComparer.InvariantCultureIgnoreCase)
            : new Dictionary<string, object?>(StringComparer.InvariantCultureIgnoreCase);

        if (handlerSettings.Settings is not null)
        {
            foreach (var setting in handlerSettings.Settings)
                settings[setting.Key] = setting.Value;
        }

        merged.Settings = settings;
        return merged;
    }

}
