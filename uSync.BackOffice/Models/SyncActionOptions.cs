using System.Collections.Generic;

using uSync.Core.Serialization;

namespace uSync.BackOffice.Models;

/// <summary>
/// Options to tell uSync how to process an action
/// </summary>
public class SyncActionOptions
{
    /// <summary>
    /// SignalR client id 
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Name of the handler to use for the action
    /// </summary>
    public string? Handler { get; set; }

    /// <summary>
    /// Should the action be forced 
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// Set to use when processing the action
    /// </summary>
    public string? Set { get; set; }

    /// <summary>
    /// SyncActions to use as the source for all individual actions
    /// </summary>
    public IEnumerable<uSyncAction> Actions { get; set; } = [];

    /// <summary>
    ///  array of usync folders you want to import - files will be merged as part of the process.
    /// </summary>
    public string[] Folders { get; set; } = [];

}

internal static class SyncActionOptionsExtensions
{
    internal static string GetSetOrDefault(this SyncActionOptions options, string defaultSet)
        => string.IsNullOrWhiteSpace(options.Set) ? defaultSet : options.Set;

    internal static string[] GetFoldersOrDefault(this SyncActionOptions options, string[] defaultFolders)
        => options.Folders.Length != 0 ? options.Folders : defaultFolders;

    internal static SerializerFlags GetImportFlags(this SyncActionOptions options)
        => options.Force ? SerializerFlags.Force : SerializerFlags.None;
}

