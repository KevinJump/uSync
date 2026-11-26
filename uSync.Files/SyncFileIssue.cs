namespace uSync.Files;

/// <summary>
///  a problem found in a uSync folder.
/// </summary>
/// <param name="Rule">the check that found it</param>
/// <param name="FilePath">full path of the file (or folder) the problem is in</param>
/// <param name="Line">1 based line number, 0 when the problem isn't on a line (e.g. a folder)</param>
/// <param name="Message">what is wrong</param>
public sealed record SyncFileIssue(SyncFileRule Rule, string FilePath, int Line, string Message)
{
    public SyncFileSeverity Severity => Rule.Severity;
}
