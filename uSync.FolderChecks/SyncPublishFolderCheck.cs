namespace uSync.FolderChecks;

/// <summary>
///  checks a publish (deploy) output has the uSync folder in it.
/// </summary>
/// <remarks>
///  forgetting to copy the uSync folder is a common deploy mistake, and the only
///  symptom is that nothing imports.
/// </remarks>
public static class SyncPublishFolderCheck
{
    private const int MaxFileIssues = 50;

    /// <param name="folder">the uSync folder in the source</param>
    /// <param name="siteRoot">the site folder the uSync folder sits in</param>
    /// <param name="publishFolder">the publish output for the site</param>
    public static IReadOnlyList<SyncFileIssue> Check(string folder, string siteRoot, string publishFolder)
    {
        var issues = new List<SyncFileIssue>();
        var root = Path.GetFullPath(folder);
        var publish = Path.GetFullPath(publishFolder);

        if (Directory.Exists(publish) is false)
        {
            issues.Add(new SyncFileIssue(SyncFileRules.PublishFolder, publish, 0,
                $"Publish folder not found: {publish}"));
            return issues;
        }

        var relative = Path.GetRelativePath(Path.GetFullPath(siteRoot), root);
        var target = Path.Combine(publish, relative);
        var display = relative.Replace('\\', '/');

        if (Directory.Exists(target) is false)
        {
            issues.Add(new SyncFileIssue(SyncFileRules.PublishFolder, root, 0,
                $"The publish output has no '{display}' folder, so the site will have nothing to import. Copy the uSync folder to the publish output (e.g. <Content Include=\"uSync\\**\" CopyToPublishDirectory=\"PreserveNewest\" /> in the .csproj)."));
            return issues;
        }

        var problems = 0;
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
        {
            var fileRelative = Path.GetRelativePath(root, file);
            var published = Path.Combine(target, fileRelative);

            string? message = null;
            if (File.Exists(published) is false)
                message = $"'{display}/{fileRelative.Replace('\\', '/')}' is missing from the publish output.";
            else if (SameContent(file, published) is false)
                message = $"'{display}/{fileRelative.Replace('\\', '/')}' in the publish output is different to the source.";

            if (message is null) continue;

            problems++;
            if (problems <= MaxFileIssues)
                issues.Add(new SyncFileIssue(SyncFileRules.PublishFolder, file, 0, message));
        }

        if (problems > MaxFileIssues)
        {
            issues.Add(new SyncFileIssue(SyncFileRules.PublishFolder, root, 0,
                $"... and {problems - MaxFileIssues} more files missing or different in the publish output."));
        }

        return issues;
    }

    private static bool SameContent(string a, string b)
    {
        var infoA = new FileInfo(a);
        var infoB = new FileInfo(b);
        if (infoA.Length != infoB.Length) return false;

        // uSync files are small, so reading them is fine.
        return File.ReadAllBytes(a).AsSpan().SequenceEqual(File.ReadAllBytes(b));
    }
}
