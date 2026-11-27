using System.Text.Json;
using System.Text.RegularExpressions;

namespace uSync.FolderChecks;

/// <summary>
///  works out where the uSync folder is, the same way the site does.
/// </summary>
public static partial class SyncFolderLocator
{
    /// <param name="Folder">the uSync folder (e.g. site/uSync/v17)</param>
    /// <param name="SiteRoot">the site the folder belongs to. The uSync folder path is
    /// relative to this, so it is what the publish folder check compares against.</param>
    public sealed record SyncFolderLocation(string Folder, string SiteRoot);

    private static readonly HashSet<string> SkipFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", "wwwroot", "umbraco", "App_Plugins", "publish"
    };

    private const int MaxSearchDepth = 3;

    /// <summary>
    ///  find the folder starting from the current directory.
    /// </summary>
    /// <remarks>
    ///  1. uSync:Settings:Folders (last) or uSync:Settings:RootFolder from appsettings.json
    ///  2. the highest uSync/v{n} folder
    ///  checked in the start folder, then in site folders (with an appsettings.json,
    ///  a .csproj or a uSync folder) up to 3 levels below it.
    /// </remarks>
    /// <exception cref="SyncFolderNotFoundException">no folder, or more than one site with one.</exception>
    public static SyncFolderLocation Discover(string startFolder)
    {
        var start = Path.GetFullPath(startFolder);
        var local = FindInSite(start);
        if (local is not null) return local;

        var found = FindSites(start, 1)
            .Select(FindInSite)
            .OfType<SyncFolderLocation>()
            .DistinctBy(x => x.Folder, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return found.Count switch
        {
            1 => found[0],
            0 => throw new SyncFolderNotFoundException(
                $"No uSync folder found in {start} (looked for uSync:Settings:RootFolder in appsettings.json and uSync/v{{n}} folders). Use --folder to say where it is."),
            _ => throw new SyncFolderNotFoundException(
                $"More than one uSync folder found, use --folder to pick one:{Environment.NewLine}  " +
                string.Join(Environment.NewLine + "  ", found.Select(x => x.Folder)))
        };
    }

    /// <summary>
    ///  a folder given on the command line - work out which site it belongs to.
    /// </summary>
    public static SyncFolderLocation FromFolder(string folder)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        if (Directory.Exists(full) is false)
            throw new SyncFolderNotFoundException($"uSync folder not found: {full}");

        var dir = new DirectoryInfo(full).Parent;
        for (var level = 0; dir is not null && level < 5; level++, dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "appsettings.json")) || dir.GetFiles("*.csproj").Length > 0)
                return new SyncFolderLocation(full, dir.FullName);
        }

        // no site found: assume the usual {site}/uSync/v{n} layout.
        var parent = new DirectoryInfo(full).Parent;
        var siteRoot = parent is not null && parent.Name.Equals("uSync", StringComparison.OrdinalIgnoreCase)
            ? parent.Parent?.FullName ?? parent.FullName
            : parent?.FullName ?? full;
        return new SyncFolderLocation(full, siteRoot);
    }

    private static SyncFolderLocation? FindInSite(string siteFolder)
    {
        var configured = ReadConfiguredFolder(Path.Combine(siteFolder, "appsettings.json"));
        if (configured is not null)
        {
            var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(siteFolder, configured)));
            if (Directory.Exists(path)) return new SyncFolderLocation(path, siteFolder);
        }

        var versioned = FindHighestVersionFolder(Path.Combine(siteFolder, "uSync"));
        return versioned is null ? null : new SyncFolderLocation(versioned, siteFolder);
    }

    private static IEnumerable<string> FindSites(string folder, int depth)
    {
        if (depth > MaxSearchDepth) yield break;

        List<string> children;
        try { children = Directory.EnumerateDirectories(folder).ToList(); }
        catch { yield break; }

        foreach (var child in children)
        {
            var name = Path.GetFileName(child);
            if (SkipFolders.Contains(name) || name.StartsWith('.')) continue;

            if (File.Exists(Path.Combine(child, "appsettings.json"))
                || Directory.Exists(Path.Combine(child, "uSync"))
                || Directory.EnumerateFiles(child, "*.csproj").Any())
            {
                yield return child;
            }

            foreach (var site in FindSites(child, depth + 1))
                yield return site;
        }
    }

    /// <summary>
    ///  the folder uSync works in, from appsettings.json (relative to the site).
    /// </summary>
    /// <remarks>
    ///  same as the site: the last of uSync:Settings:Folders, else uSync:Settings:RootFolder.
    /// </remarks>
    public static string? ReadConfiguredFolder(string appSettingsFile)
    {
        if (File.Exists(appSettingsFile) is false) return null;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(appSettingsFile), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (TryGetProperty(doc.RootElement, "uSync", out var usync) is false
                || TryGetProperty(usync, "Settings", out var settings) is false)
                return null;

            if (TryGetProperty(settings, "Folders", out var folders)
                && folders.ValueKind == JsonValueKind.Array
                && folders.GetArrayLength() > 0
                && folders[folders.GetArrayLength() - 1].ValueKind == JsonValueKind.String
                && string.IsNullOrWhiteSpace(folders[folders.GetArrayLength() - 1].GetString()) is false)
            {
                return CleanPath(folders[folders.GetArrayLength() - 1].GetString()!);
            }

            if (TryGetProperty(settings, "RootFolder", out var root)
                && root.ValueKind == JsonValueKind.String
                && string.IsNullOrWhiteSpace(root.GetString()) is false)
            {
                return CleanPath(root.GetString()!);
            }
        }
        catch (JsonException)
        {
            // not our job to validate appsettings.json.
        }

        return null;
    }

    /// <summary>
    ///  the uSync/v{n} folder with the highest n.
    /// </summary>
    public static string? FindHighestVersionFolder(string uSyncFolder)
    {
        if (Directory.Exists(uSyncFolder) is false) return null;

        return Directory.EnumerateDirectories(uSyncFolder)
            .Select(x => (Path: x, Match: VersionFolderRegex().Match(Path.GetFileName(x))))
            .Where(x => x.Match.Success)
            .OrderByDescending(x => int.Parse(x.Match.Groups[1].Value))
            .Select(x => x.Path)
            .FirstOrDefault();
    }

    private static string CleanPath(string path)
        => path.Replace('\\', '/').TrimStart('~').TrimStart('/');

    // configuration keys are case insensitive.
    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }
        value = default;
        return false;
    }

    [GeneratedRegex(@"^v(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex VersionFolderRegex();
}

public class SyncFolderNotFoundException(string message) : Exception(message);
