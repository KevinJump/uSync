using System.Xml;
using System.Xml.Linq;

namespace uSync.Files;

public sealed class SyncFolderVerifyOptions
{
    /// <summary>
    ///  also check the Content, Media and Blueprints folders.
    /// </summary>
    public bool IncludeContent { get; init; }
}

public sealed class SyncFolderVerifyResult
{
    public required string Folder { get; init; }

    public required IReadOnlyList<SyncFileIssue> Issues { get; init; }

    /// <summary>
    ///  the .config files that loaded.
    /// </summary>
    public required IReadOnlyList<SyncFileItem> Items { get; init; }

    /// <summary>
    ///  number of files looked at.
    /// </summary>
    public int FileCount { get; init; }

    /// <summary>
    ///  folders that were not checked (content folders without --content).
    /// </summary>
    public IReadOnlyList<string> SkippedFolders { get; init; } = [];

    public int ErrorCount => Issues.Count(x => x.Severity == SyncFileSeverity.Error);
    public int WarningCount => Issues.Count(x => x.Severity == SyncFileSeverity.Warning);
}

/// <summary>
///  checks a uSync folder for problems that would break (or quietly spoil) an import.
/// </summary>
/// <remarks>
///  works on the files alone - no site or database - so it can run on a pull request.
/// </remarks>
public class SyncFolderVerifier
{
    /// <summary>
    ///  folders only checked with IncludeContent.
    /// </summary>
    public static readonly IReadOnlySet<string> ContentFolders =
        new HashSet<string>(["Content", "Media", "Blueprints"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///  folders where the alias is the node name, so doesn't have to be unique.
    /// </summary>
    private static readonly HashSet<string> NodeNameFolders =
        new(["Content", "Media", "Blueprints", "ContentTemplates"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> BlueprintFolders =
        new(["Blueprints", "ContentTemplates"], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///  folders uSync and its add-ons write. Other folders are fine when their
    ///  files look like uSync items (have a Key), so custom handlers don't warn.
    /// </summary>
    private static readonly HashSet<string> KnownFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Blueprints", "Content", "ContentTemplates", "ContentTypes", "DataTypes", "Dictionary",
        "Domains", "Languages", "Media", "MediaTypes", "MemberTypes", "Members", "RelationTypes",
        "Relations", "Templates", "Webhooks", "Users", "UserGroups", "Forms", "FormsDataSources",
        "FormsPrevalueSources", "FormsWorkflows", "PublicAccess", "Macros", "Stylesheets", "Scripts",
        "PartialViews", "Settings"
    };

    public const string ConfigFileName = "usync.config";

    public SyncFolderVerifyResult Verify(string folder, SyncFolderVerifyOptions? options = null)
    {
        options ??= new SyncFolderVerifyOptions();
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        if (Directory.Exists(root) is false)
            throw new SyncFolderNotFoundException($"uSync folder not found: {root}");

        var issues = new List<SyncFileIssue>();
        var items = new List<SyncFileItem>();
        var skipped = new List<string>();
        var fileCount = 0;

        foreach (var file in Directory.EnumerateFiles(root).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (IsHidden(file)) continue;
            fileCount++;
            var text = File.ReadAllText(file);
            var conflicts = CheckConflicts(file, text, issues);

            if (Path.GetFileName(file).Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                var doc = Load(file, text, conflicts, issues);
                if (doc?.Root is not null) CheckFormatVersion(root, file, doc.Root, issues);
                continue;
            }

            issues.Add(new SyncFileIssue(SyncFileRules.UnknownFile, file, 0,
                $"'{Path.GetFileName(file)}' is not a uSync file (only usync.config belongs at the top of the folder)."));
        }

        foreach (var typeFolder in Directory.EnumerateDirectories(root).Order(StringComparer.OrdinalIgnoreCase))
        {
            var typeName = Path.GetFileName(typeFolder);
            if (typeName.StartsWith('.')) continue;

            if (options.IncludeContent is false && ContentFolders.Contains(typeName))
            {
                skipped.Add(typeName);
                continue;
            }

            var folderItems = new List<SyncFileItem>();
            var folderFiles = 0;

            foreach (var file in Directory.EnumerateFiles(typeFolder, "*", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
            {
                if (IsHidden(file)) continue;
                fileCount++;
                folderFiles++;

                var text = File.ReadAllText(file);
                var conflicts = CheckConflicts(file, text, issues);

                if (Path.GetExtension(file).Equals(".config", StringComparison.OrdinalIgnoreCase) is false)
                {
                    issues.Add(new SyncFileIssue(SyncFileRules.UnknownFile, file, 0,
                        $"'{GetRelativePath(root, file)}' is not a .config file, uSync will ignore it."));
                    continue;
                }

                var doc = Load(file, text, conflicts, issues);
                if (doc?.Root is null) continue;

                folderItems.Add(SyncFileItem.Create(doc.Root, file, GetRelativePath(root, file), typeName));
            }

            if (folderFiles > 0
                && KnownFolders.Contains(typeName) is false
                && folderItems.Any(x => x.Key != Guid.Empty) is false)
            {
                issues.Add(new SyncFileIssue(SyncFileRules.UnknownFile, typeFolder, 0,
                    $"'{typeName}' is not a uSync folder (none of its files are uSync items)."));
            }

            items.AddRange(folderItems);
        }

        foreach (var group in items.GroupBy(x => x.TypeFolder, StringComparer.OrdinalIgnoreCase))
        {
            var typeItems = group.ToList();
            CheckDuplicateKeys(typeItems, issues);
            if (NodeNameFolders.Contains(group.Key) is false)
                CheckAliasClashes(typeItems, issues);
            CheckDeleteAndLive(typeItems, issues);
        }

        CheckReferences(items, issues);

        if (options.IncludeContent)
            CheckContentTypes(items, issues);

        return new SyncFolderVerifyResult
        {
            Folder = root,
            Issues = issues
                .OrderBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Line)
                .ToList(),
            Items = items,
            FileCount = fileCount,
            SkippedFolders = skipped,
        };
    }

    private static bool IsHidden(string file) => Path.GetFileName(file).StartsWith('.');

    private static IReadOnlyList<ConflictMarkerScanner.ConflictBlock> CheckConflicts(string file, string text, List<SyncFileIssue> issues)
    {
        var blocks = ConflictMarkerScanner.Scan(text);
        foreach (var block in blocks)
        {
            var lines = block.StartLine == block.EndLine ? $"line {block.StartLine}" : $"lines {block.StartLine}-{block.EndLine}";
            issues.Add(new SyncFileIssue(SyncFileRules.MergeConflict, file, block.StartLine,
                $"Unresolved merge conflict ({lines})."));
        }
        return blocks;
    }

    private static XDocument? Load(string file, string text, IReadOnlyList<ConflictMarkerScanner.ConflictBlock> conflicts, List<SyncFileIssue> issues)
    {
        try
        {
            var doc = XDocument.Parse(text, LoadOptions.SetLineInfo);

            if (conflicts.Count > 0)
            {
                // the markers are inside a CDATA block or a value, so the
                // file loads - which is why this is worth checking for.
                for (var n = issues.Count - 1; n >= 0 && n >= issues.Count - conflicts.Count; n--)
                {
                    if (issues[n].Rule == SyncFileRules.MergeConflict && issues[n].FilePath == file)
                    {
                        issues[n] = issues[n] with
                        {
                            Message = issues[n].Message + " The file is still valid XML, so uSync would import the conflicted value as it is."
                        };
                    }
                }
            }

            return doc;
        }
        catch (XmlException ex)
        {
            // conflict markers already explain why it won't load.
            if (conflicts.Count == 0)
            {
                issues.Add(new SyncFileIssue(SyncFileRules.InvalidXml, file, Math.Max(ex.LineNumber, 1),
                    $"Not valid XML: {ex.Message}"));
            }
            return null;
        }
    }

    /// <summary>
    ///  two live items of the same type with the same key.
    /// </summary>
    private static void CheckDuplicateKeys(List<SyncFileItem> items, List<SyncFileIssue> issues)
    {
        var duplicates = items
            .Where(x => x.IsEmpty is false && x.Key != Guid.Empty)
            .GroupBy(x => x.Key)
            .Where(x => x.Count() > 1);

        foreach (var group in duplicates)
        {
            foreach (var item in group)
            {
                var others = string.Join(", ", group.Where(x => x != item).Select(x => x.RelativePath));
                issues.Add(new SyncFileIssue(SyncFileRules.DuplicateKey, item.FilePath, item.Line,
                    $"Key {item.Key} is also used by {others}. Usually a copy and paste or a bad merge."));
            }
        }
    }

    /// <summary>
    ///  two live items of the same type with the same alias and different keys,
    ///  the classic "both branches added 'article'" clash.
    /// </summary>
    private static void CheckAliasClashes(List<SyncFileItem> items, List<SyncFileIssue> issues)
    {
        var clashes = items
            .Where(x => x.IsEmpty is false && string.IsNullOrWhiteSpace(x.Alias) is false)
            .GroupBy(x => x.Alias, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Select(i => i.Key).Distinct().Count() > 1);

        foreach (var group in clashes)
        {
            foreach (var item in group)
            {
                var others = string.Join(", ", group.Where(x => x.Key != item.Key).Select(x => $"{x.RelativePath} ({x.Key})"));
                issues.Add(new SyncFileIssue(SyncFileRules.AliasClash, item.FilePath, item.Line,
                    $"Alias '{item.Alias}' ({item.Key}) is also used by {others}. Only one can be imported - often two branches each created '{item.Alias}'."));
            }
        }
    }

    /// <summary>
    ///  an item that is deleted and also there, uSync would report this as a
    ///  duplicate at import time.
    /// </summary>
    private static void CheckDeleteAndLive(List<SyncFileItem> items, List<SyncFileIssue> issues)
    {
        var live = items
            .Where(x => x.IsEmpty is false && x.Key != Guid.Empty)
            .ToLookup(x => x.Key);

        foreach (var deleted in items.Where(x => x.IsDelete && x.Key != Guid.Empty))
        {
            var matches = live[deleted.Key].ToList();
            if (matches.Count == 0) continue;

            issues.Add(new SyncFileIssue(SyncFileRules.DeleteAndLive, deleted.FilePath, deleted.Line,
                $"'{deleted.Alias}' ({deleted.Key}) is marked as deleted, but {string.Join(", ", matches.Select(x => x.RelativePath))} still has it. The import would both create and delete it."));

            foreach (var match in matches)
            {
                issues.Add(new SyncFileIssue(SyncFileRules.DeleteAndLive, match.FilePath, match.Line,
                    $"'{match.Alias}' ({match.Key}) is also marked as deleted by {deleted.RelativePath}. The import would both create and delete it."));
            }
        }
    }

    /// <summary>
    ///  keys that items point at, but no file defines.
    /// </summary>
    /// <remarks>
    ///  often fine (the item is already in the target database), so these are warnings,
    ///  but it can mean a file is missing from the commit.
    /// </remarks>
    private static void CheckReferences(List<SyncFileItem> items, List<SyncFileIssue> issues)
    {
        var live = items.Where(x => x.IsEmpty is false).ToList();
        var keys = live.Where(x => x.Key != Guid.Empty).Select(x => x.Key).ToHashSet();
        var templateAliases = live
            .Where(x => x.ElementName == "Template")
            .Select(x => x.Alias)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in live)
        {
            foreach (var (node, kind, key, name) in GetReferences(item))
            {
                if (key == Guid.Empty || keys.Contains(key)) continue;

                var label = string.IsNullOrWhiteSpace(name) ? key.ToString() : $"'{name}' ({key})";
                issues.Add(new SyncFileIssue(SyncFileRules.BrokenReference, item.FilePath, SyncFileItem.GetLine(node),
                    $"{item.ElementName} '{item.Alias}' uses {kind} {label}, which isn't in the uSync folder."));
            }

            if (item.ElementName == "Template")
            {
                var parent = item.Node.Element("Parent");
                if (parent is not null
                    && string.IsNullOrWhiteSpace(parent.Value) is false
                    && templateAliases.Contains(parent.Value.Trim()) is false)
                {
                    issues.Add(new SyncFileIssue(SyncFileRules.BrokenReference, item.FilePath, SyncFileItem.GetLine(parent),
                        $"Template '{item.Alias}' has parent template '{parent.Value.Trim()}', which isn't in the uSync folder."));
                }
            }
        }
    }

    private static IEnumerable<(XElement Node, string Kind, Guid Key, string? Name)> GetReferences(SyncFileItem item)
    {
        var info = item.Node.Element("Info");
        if (info is not null)
        {
            var compositions = info.Element("Compositions")?.Elements("Composition") ?? [];
            foreach (var composition in compositions)
                yield return (composition, "composition", ReadKey(composition.Attribute("Key")?.Value), composition.Value);

            // a blueprint's parent is a blueprint folder (a container), which
            // uSync doesn't write a file for.
            var parent = info.Element("Parent");
            if (parent is not null && BlueprintFolders.Contains(item.TypeFolder) is false)
                yield return (parent, "parent", ReadKey(parent.Attribute("Key")?.Value), parent.Value);

            var templates = info.Element("AllowedTemplates")?.Elements("Template") ?? [];
            foreach (var template in templates)
                yield return (template, "template", ReadKey(template.Attribute("Key")?.Value), template.Value);

            var listView = info.Element("ListView");
            if (listView is not null)
                yield return (listView, "list view data type", ReadKey(listView.Value), null);
        }

        var children = item.Node.Element("Structure")?.Elements() ?? [];
        foreach (var child in children)
            yield return (child, "allowed child", ReadKey(child.Attribute("Key")?.Value), child.Value);

        var properties = item.Node.Element("GenericProperties")?.Elements("GenericProperty") ?? [];
        foreach (var property in properties)
        {
            var definition = property.Element("Definition");
            if (definition is null) continue;
            var alias = property.Element("Alias")?.Value;
            yield return (definition, $"data type (property '{alias}')", ReadKey(definition.Value), null);
        }
    }

    /// <summary>
    ///  content (and blueprints, media) whose content type isn't in the folder -
    ///  import fails for those.
    /// </summary>
    private static void CheckContentTypes(List<SyncFileItem> items, List<SyncFileIssue> issues)
    {
        var live = items.Where(x => x.IsEmpty is false).ToList();

        var contentTypes = live
            .Where(x => x.ElementName == "ContentType")
            .Select(x => x.Alias)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var mediaTypes = live
            .Where(x => x.ElementName == "MediaType")
            .Select(x => x.Alias)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in live.Where(x => x.ElementName is "Content" or "Media"))
        {
            var typeNode = item.Node.Element("Info")?.Element("ContentType");
            var alias = typeNode?.Value.Trim();
            if (typeNode is null || string.IsNullOrWhiteSpace(alias)) continue;

            var (types, kind, folder) = item.ElementName == "Media"
                ? (mediaTypes, "media type", "MediaTypes")
                : (contentTypes, "content type", "ContentTypes");

            if (types.Contains(alias)) continue;

            issues.Add(new SyncFileIssue(SyncFileRules.MissingContentType, item.FilePath, SyncFileItem.GetLine(typeNode),
                $"{item.ElementName} '{item.Alias}' uses {kind} '{alias}', which has no file in {folder}. The import will fail for this item unless the site already has it."));
        }
    }

    /// <summary>
    ///  files written by an older major version of uSync than the site uses.
    /// </summary>
    private static void CheckFormatVersion(string root, string file, XElement node, List<SyncFileIssue> issues)
    {
        var fileVersion = SyncPackageVersionFinder.ParseVersion(node.Attribute("version")?.Value);
        if (fileVersion is null) return;

        var package = SyncPackageVersionFinder.Find(root);
        if (package is null) return;

        if (fileVersion.Major < package.Value.Version.Major)
        {
            issues.Add(new SyncFileIssue(SyncFileRules.OldFormat, file, SyncFileItem.GetLine(node),
                $"The files were exported by uSync {fileVersion}, but {Path.GetFileName(package.Value.ProjectFile)} uses uSync {package.Value.Version}. Run a full export from the site to update them."));
        }
    }

    private static Guid ReadKey(string? value)
        => Guid.TryParse(value?.Trim(), out var key) ? key : Guid.Empty;

    public static string GetRelativePath(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');
}
