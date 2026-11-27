using uSync.FolderChecks;

namespace uSync.FolderChecks.Tests;

internal static class Fixtures
{
    /// <summary>
    ///  the uSync folder of a fixture (Fixtures/{name}/uSync/v17).
    /// </summary>
    public static string Folder(string name)
        => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name, "uSync", "v17");

    public static string Site(string name)
        => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name);

    public static SyncFolderVerifyResult Verify(string name, bool content = false)
        => new SyncFolderVerifier().Verify(Folder(name), new SyncFolderVerifyOptions { IncludeContent = content });

    public static List<SyncFileIssue> Issues(this SyncFolderVerifyResult result, SyncFileRule rule)
        => result.Issues.Where(x => x.Rule == rule).ToList();

    public static string Relative(this SyncFileIssue issue, string name)
        => Path.GetRelativePath(Folder(name), issue.FilePath).Replace('\\', '/');

    /// <summary>
    ///  copy a fixture to a temp folder, for tests that change files.
    /// </summary>
    public static string CopyToTemp(string name)
    {
        var target = TempFolder();
        CopyDirectory(Site(name), target);
        return target;
    }

    public static string TempFolder()
    {
        var path = Path.Combine(Path.GetTempPath(), "uSyncFilesTests", Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(path);
        return path;
    }

    public static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
    }

    public static void DeleteFolder(string path)
    {
        if (Directory.Exists(path) is false) return;

        // git makes its object files read only.
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        try { Directory.Delete(path, true); } catch { /* temp folder, not fatal */ }
    }
}
