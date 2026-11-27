using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace uSync.FolderChecks;

/// <summary>
///  finds the version of uSync a site uses, from the .csproj near the uSync folder.
/// </summary>
public static partial class SyncPackageVersionFinder
{
    private static readonly string[] PackageIds = ["uSync", "uSync.BackOffice", "uSync.Core", "uSync.Complete"];

    private const int MaxLevels = 6;

    /// <summary>
    ///  walk up from the uSync folder to the first folder with a .csproj in it,
    ///  and read the uSync package version from that.
    /// </summary>
    /// <returns>the version and the project it came from, or null if there isn't one.</returns>
    public static (Version Version, string ProjectFile)? Find(string folder)
    {
        var dir = new DirectoryInfo(folder);
        for (var level = 0; dir is not null && level < MaxLevels; level++, dir = dir.Parent)
        {
            var projects = dir.GetFiles("*.csproj");
            if (projects.Length > 0)
            {
                foreach (var project in projects)
                {
                    var version = ReadProjectVersion(project.FullName);
                    if (version is not null) return (version, project.FullName);
                }

                // stop at the first project, that is the site.
                return null;
            }

            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) return null;
        }

        return null;
    }

    private static Version? ReadProjectVersion(string projectFile)
    {
        XDocument doc;
        try { doc = XDocument.Load(projectFile); }
        catch { return null; }

        foreach (var reference in doc.Descendants().Where(x => x.Name.LocalName == "PackageReference"))
        {
            var id = reference.Attribute("Include")?.Value ?? reference.Attribute("Update")?.Value;
            if (id is null || IsuSyncPackage(id) is false) continue;

            var versionText = reference.Attribute("Version")?.Value
                ?? reference.Attribute("VersionOverride")?.Value
                ?? reference.Elements().FirstOrDefault(x => x.Name.LocalName == "Version")?.Value;

            var version = versionText is null
                ? FindCentralVersion(Path.GetDirectoryName(projectFile)!, id)
                : ParseVersion(versionText);

            if (version is not null) return version;
        }

        return null;
    }

    private static Version? FindCentralVersion(string folder, string packageId)
    {
        var dir = new DirectoryInfo(folder);
        for (var level = 0; dir is not null && level < MaxLevels; level++, dir = dir.Parent)
        {
            var props = Path.Combine(dir.FullName, "Directory.Packages.props");
            if (File.Exists(props))
            {
                try
                {
                    var doc = XDocument.Load(props);
                    var entry = doc.Descendants()
                        .Where(x => x.Name.LocalName == "PackageVersion")
                        .FirstOrDefault(x => string.Equals(x.Attribute("Include")?.Value, packageId, StringComparison.OrdinalIgnoreCase));

                    return ParseVersion(entry?.Attribute("Version")?.Value);
                }
                catch
                {
                    return null;
                }
            }

            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) return null;
        }
        return null;
    }

    private static bool IsuSyncPackage(string id)
        => PackageIds.Any(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///  read the leading numbers of a version (17.0.4, [17.0,18), 17.*, 17.1.0-rc1).
    /// </summary>
    public static Version? ParseVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = VersionRegex().Match(value);
        if (match.Success is false) return null;

        var major = int.Parse(match.Groups[1].Value);
        var minor = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
        var build = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
        return new Version(major, minor, build);
    }

    [GeneratedRegex(@"^\s*[\[\(]?\s*(\d+)(?:\.(\d+))?(?:\.(\d+))?")]
    private static partial Regex VersionRegex();
}
