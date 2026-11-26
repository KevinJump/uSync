namespace uSync.Files;

public enum SyncFileSeverity
{
    Warning,
    Error
}

/// <summary>
///  a check that files-verify runs.
/// </summary>
/// <param name="Id">stable id, used in sarif / junit / github output (e.g. USYNC001)</param>
/// <param name="Name">short kebab-case name</param>
/// <param name="Severity">default severity</param>
/// <param name="Description">one line description of what the check looks for</param>
public sealed record SyncFileRule(string Id, string Name, SyncFileSeverity Severity, string Description);

public static class SyncFileRules
{
    public static readonly SyncFileRule MergeConflict = new("USYNC001", "merge-conflict", SyncFileSeverity.Error,
        "File contains unresolved merge conflict markers");

    public static readonly SyncFileRule InvalidXml = new("USYNC002", "invalid-xml", SyncFileSeverity.Error,
        ".config file is not valid XML");

    public static readonly SyncFileRule DuplicateKey = new("USYNC003", "duplicate-key", SyncFileSeverity.Error,
        "Two files of the same type share a Key");

    public static readonly SyncFileRule AliasClash = new("USYNC004", "alias-clash", SyncFileSeverity.Error,
        "Two items of the same type share an Alias but have different Keys");

    public static readonly SyncFileRule DeleteAndLive = new("USYNC005", "delete-and-live", SyncFileSeverity.Error,
        "An item is both deleted (<Empty Change=\"Delete\">) and present in the same folder");

    public static readonly SyncFileRule BrokenReference = new("USYNC101", "broken-reference", SyncFileSeverity.Warning,
        "An item references a Key that no file in the folder defines");

    public static readonly SyncFileRule OldFormat = new("USYNC102", "old-format", SyncFileSeverity.Warning,
        "usync.config was written by an older uSync than the site uses");

    public static readonly SyncFileRule UnknownFile = new("USYNC103", "unknown-file", SyncFileSeverity.Warning,
        "Unknown folder, or a file that isn't a .config file");

    public static readonly SyncFileRule MissingContentType = new("USYNC104", "missing-content-type", SyncFileSeverity.Warning,
        "Content or media item whose content type has no file in the folder");

    public static readonly SyncFileRule PublishFolder = new("USYNC201", "publish-folder", SyncFileSeverity.Error,
        "The publish output is missing the uSync folder, or files in it");

    public static IReadOnlyList<SyncFileRule> All { get; } =
    [
        MergeConflict, InvalidXml, DuplicateKey, AliasClash, DeleteAndLive,
        BrokenReference, OldFormat, UnknownFile, MissingContentType, PublishFolder
    ];
}
