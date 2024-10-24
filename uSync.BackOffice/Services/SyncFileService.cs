using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Extensions;

using uSync.Core;
using uSync.Core.Tracking;

namespace uSync.BackOffice.Services;

/// <summary>
///  putting all file actions in a service, 
///  so if we want to abstract later we can.
/// </summary>
internal class SyncFileService : ISyncFileService
{
    private readonly ILogger<SyncFileService> _logger;
    private readonly IHostEnvironment _hostEnvironment;

    private static readonly char[] _trimChars = [' ', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <summary>
    /// Constructor for File service (via DI)
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="hostEnvironment"></param>
    public SyncFileService(ILogger<SyncFileService> logger,
                           IHostEnvironment hostEnvironment)
    {
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    /// <inheritdoc/>
    public string GetAbsPath(string path)
    {
        if (Path.IsPathFullyQualified(path)) return CleanLocalPath(path);
        return CleanLocalPath(_hostEnvironment.MapPathContentRoot(path.TrimStart(_trimChars)));
    }

    /// <inheritdoc/>
    public string GetSiteRelativePath(string path)
    {
        if (Path.IsPathFullyQualified(path) && path.StartsWith(_hostEnvironment.ContentRootPath))
            return path.Substring(_hostEnvironment.ContentRootPath.Length).TrimStart(_trimChars);
        return path;
    }

    /// <inheritdoc/>
    private static string CleanLocalPath(string path)
        => Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

    /// <inheritdoc/>
    public bool FileExists(string path)
        => File.Exists(GetAbsPath(path));

    /// <inheritdoc/>
    public bool PathMatches(string a, string b)
        => GetAbsPath(a).Equals(GetAbsPath(b), StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    ///  does a directory exist
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool DirectoryExists(string path)
        => Directory.Exists(GetAbsPath(path));

    /// <inheritdoc/>
    public bool DirectoryHasChildren(string path)
    {
        var fullPath = GetAbsPath(path);
        if (Directory.Exists(fullPath) is false) return false;
        return Directory.GetDirectories(fullPath).Length > 0;
    }

    /// <inheritdoc/>
    public void DeleteFile(string path)
    {
        var localPath = GetAbsPath(path);
        if (FileExists(localPath))
            File.Delete(localPath);
    }

    /// <inheritdoc/>
    public void EnsureFileExists(string path)
    {
        if (!FileExists(path))
            throw new FileNotFoundException("Missing File", path);
    }

    /// <inheritdoc/>
    public FileStream? OpenRead(string path)
    {
        var localPath = GetAbsPath(path);

        if (!FileExists(localPath)) return null;
        return File.OpenRead(localPath);
    }

    /// <inheritdoc/>
    public FileStream OpenWrite(string path)
    {
        var localPath = GetAbsPath(path);

        if (FileExists(localPath))
            DeleteFile(localPath);

        CreateFoldersForFile(path);
        return File.OpenWrite(localPath);
    }

    /// <inheritdoc/>
    public void CopyFile(string source, string target)
    {
        var absSource = GetAbsPath(source);
        var absTarget = GetAbsPath(target);

        var directoryName = Path.GetDirectoryName(absTarget);
        if (string.IsNullOrEmpty(directoryName))
            throw new DirectoryNotFoundException($"Cannot find directory for {absTarget}");

        Directory.CreateDirectory(directoryName);
        File.Copy(absSource, absTarget, true);
    }

    /// <inheritdoc/>
    public void CreateFoldersForFile(string filePath)
    {
        var absPath = Path.GetDirectoryName(GetAbsPath(filePath));
        if (string.IsNullOrEmpty(absPath)) return;

        if (Directory.Exists(absPath) is false)
            Directory.CreateDirectory(absPath);
    }

    /// <inheritdoc/>
    public void CreateFolder(string folder)
    {
        var absPath = GetAbsPath(folder);
        if (!Directory.Exists(absPath))
            Directory.CreateDirectory(absPath);
    }

    /// <inheritdoc/>
    public void CleanFolder(string folder)
    {
        var absPath = GetAbsPath(folder);

        if (Directory.Exists(absPath))
            Directory.Delete(absPath, true);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetFiles(string folder, string extensions)
        => GetFiles(folder, extensions, false);

    /// <inheritdoc/>
    public IEnumerable<string> GetFiles(string folder, string extensions, bool allFolders)
    {
        var localPath = GetAbsPath(folder);

        if (!DirectoryExists(localPath)) return [];

        return Directory.GetFiles(localPath, extensions, allFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetDirectories(string folder)
    {
        var localPath = GetAbsPath(folder);
        if (!DirectoryExists(localPath)) return [];

        return Directory.GetDirectories(localPath);

    }

    /// <inheritdoc/>
    public async Task<XElement> LoadXElementAsync(string file)
    {
        EnsureFileExists(file);

        try
        {
            using (var stream = OpenRead(file))
            {
                if (stream is null)
                    throw new FileNotFoundException($"Cannot create stream for {file}"); ;

                return await XElement.LoadAsync(stream, LoadOptions.PreserveWhitespace, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error while reading in {file} {message}", file, ex.Message);
            throw new Exception($"Error while reading in {file}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task SaveFileAsync(string filename, Stream stream)
    {
        _logger.LogDebug("Saving File: {file}", filename);

        using (Stream fileStream = OpenWrite(filename))
        {
            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
            fileStream.Close();
        }
    }

    /// <inheritdoc/>
    public async Task SaveFileAsync(string filename, string content)
    {
        var localFile = GetAbsPath(filename);
        _logger.LogDebug("Saving File: {local} [{length}]", localFile, content.Length);

        using (Stream stream = OpenWrite(localFile))
        {
            byte[] info = new UTF8Encoding(true).GetBytes(content);
            await stream.WriteAsync(info, 0, info.Length);
            await stream.FlushAsync();
            stream.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task SaveXElementAsync(XElement node, string filename)
    {
        var localPath = GetAbsPath(filename);
        using (var stream = OpenWrite(localPath))
        {
            await node.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
            await stream.FlushAsync();
            stream.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<string> LoadContentAsync(string file)
    {
        if (FileExists(file))
        {
            var absPath = this.GetAbsPath(file);
            return await File.ReadAllTextAsync(absPath);
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public void DeleteFolder(string folder, bool safe = false)
    {
        try
        {
            var resolvedFolder = GetAbsPath(folder);
            if (Directory.Exists(resolvedFolder))
                Directory.Delete(resolvedFolder, true);
        }
        catch (Exception ex)
        {
            // can happen when its locked, question is - do you care?
            _logger.LogWarning(ex, "Failed to remove directory {folder}", folder);
            if (!safe) throw;
        }
    }

    /// <inheritdoc/>
    public void CopyFolder(string source, string target)
    {
        var resolvedSource = GetAbsPath(source).TrimEnd(Path.DirectorySeparatorChar); ;
        var resolvedTarget = GetAbsPath(target).TrimEnd(Path.DirectorySeparatorChar);

        if (!Directory.Exists(resolvedSource))
            throw new DirectoryNotFoundException(source);

        Directory.CreateDirectory(resolvedTarget);

        // create all the sub folders 
        var folders = Directory.GetDirectories(resolvedSource, "*", SearchOption.AllDirectories);
        foreach (var folder in folders)
        {
            Directory.CreateDirectory(folder.Replace(resolvedSource, resolvedTarget));
        }

        // copy all the files
        var files = Directory.GetFiles(resolvedSource, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            File.Copy(file, file.Replace(resolvedSource, resolvedTarget), true);
        }

    }

    /// <inheritdoc/>
    public List<string> VerifyFolder(string folder, string extension)
    {
        var resolvedFolder = GetAbsPath(folder);
        if (!DirectoryExists(resolvedFolder))
            throw new DirectoryNotFoundException(folder);

        var keys = new Dictionary<string, string>();
        var errors = new List<string>();

        var files = Directory.GetFiles(resolvedFolder, $"*.{extension}", SearchOption.AllDirectories)
                        .ToList();

        if (files.Count == 0)
        {
            errors.Add($"There are no files with extension .{extension} in this zip file, is it even an import?");
            return errors;
        }


        foreach (var file in files)
        {
            try
            {
                var node = XElement.Load(file);

                if (!node.IsEmptyItem())
                {
                    // make the key unique for the type, then we don't get false
                    // positives when different bits share ids (like PublicAccess and Content)

                    var key = $"{node.Name.LocalName}_{node.GetKey()}";
                    var folderName = Path.GetFileName(Path.GetDirectoryName(file));
                    var filename = Path.GetFileName(file);
                    var filePath = GetShortFileName(file);

                    if (keys.TryGetValue(key, out string? value))
                    {
                        errors.Add($"Clash {filePath} shares an id with {value}");
                    }
                    else
                    {
                        keys[key] = filePath;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{GetShortFileName(file)} is invalid {ex.Message}");
            }
        }

        return errors;
    }

    static string GetShortFileName(string file)
        => $"{Path.DirectorySeparatorChar}{Path.GetFileName(Path.GetDirectoryName(file))}" +
        $"{Path.DirectorySeparatorChar}{Path.GetFileName(file)}";

    // roots 
    #region roots

    /// <inheritdoc/>
    public async Task<IEnumerable<OrderedNodeInfo>> MergeFoldersAsync(string[] folders, string extension, ISyncTrackerBase? trackerBase)
    {
        var elements = new Dictionary<string, OrderedNodeInfo>();
        var cleanElements = new Dictionary<string, OrderedNodeInfo>();

        foreach (var folder in folders)
        {
            var absPath = GetAbsPath(folder);

            if (DirectoryExists(absPath) is false) continue;

            var items = await GetFolderItemsAsync(absPath, extension);

            var localKeys = new List<Guid>();

            foreach (var item in items)
            {
                var itemKey = item.Value.Node.GetKey();
                if (item.Value.Node.IsEmptyItem() is false)
                {
                    if (localKeys.Contains(itemKey))
                    {
                        throw new Exception($"Duplicate: Item key {itemKey} already exists for {item.Key} - run uSync Health check for more info.");
                    }
                    localKeys.Add(itemKey);

                    if (elements.TryGetValue(item.Key, out var value))
                    {
                        // merge these files.
                        item.Value.SetNode(MergeNodes(value.Node, item.Value.Node, trackerBase));
                        item.Value.SetFileName($"{uSyncConstants.MergedFolderName}/{Path.GetFileName(item.Value.FileName)}");
                    }
                }
                else
                {
                    switch (item.Value.Node.GetEmptyAction())
                    {
                        case SyncActionType.Delete:
                            // deletes get added, but there can be duplicate deletes, 
                            // we don't care, we just need one, (so we can add them multiple times).
                            break;
                        case SyncActionType.Clean:
                            // cleans are added, these run a clean up at the end, so if they exist 
                            // we need to add them, but they can clash in terms of keys. 
                            _ = cleanElements.TryAdd(item.Key, item.Value);
                            continue;
                        case SyncActionType.Rename: // renames are just markers to make sure they don't leave things on disk.
                        case SyncActionType.None: // none should never happen, we can ignore them..
                        default:
                            continue;
                    }
                }

                elements[item.Key] = item.Value;
            }
        }

        return [.. elements.Values, .. cleanElements.Values];
    }

    /// <inheritdoc/>
    public async Task<XElement?> MergeFilesAsync(string[] filenames, ISyncTrackerBase? trackerBase)
    {
        if (filenames.Length == 0) return null;
        var latest = await LoadXElementSafeAsync(filenames[0]);
        if (filenames.Length == 1 || latest is null) return latest;

        for (var n = 1; n < filenames.Length; n++)
        {
            var node = await LoadXElementSafeAsync(filenames[n]);
            if (node is null) continue;
            latest = MergeNodes(latest, node, trackerBase);
        }
        return latest;
    }

    private XElement MergeNodes(XElement source, XElement target, ISyncTrackerBase? trackerBase)
        => trackerBase is null ? target : trackerBase.MergeFiles(source, target) ?? target;


    private async Task<IEnumerable<KeyValuePair<string, OrderedNodeInfo>>> GetFolderItemsAsync(string folder, string extension)
    {
        var items = new List<KeyValuePair<string, OrderedNodeInfo>>();

        foreach (var file in GetFilePaths(folder, extension))
        {
            var element = await LoadXElementSafeAsync(file);
            if (element != null)
            {
                var path = file.Substring(folder.Length);

                items.Add(new KeyValuePair<string, OrderedNodeInfo>(
                    key: path,
                    value: new OrderedNodeInfo(
                        filename: file,
                        node: element,
                        level: (element.GetLevel() * 1000) + element.GetItemSortOrder(),
                        path: path,
                        isRoot: true)));
            }
        }

        return items;
    }

    /// <inheritdoc/>
    public async Task<XElement?> GetNearestNodeAsync(string filePath, string[] folders)
    {
        foreach (var folder in folders.Reverse())
        {
            var path = Path.Combine(folder, filePath);
            if (FileExists(path))
                return await LoadXElementSafeAsync(path);
        }

        return null;
    }

    /// <inheritdoc/>
    public XElement? GetDifferences(List<XElement> nodes, ISyncTrackerBase? trackerBase)
    {
        if (nodes is null || nodes?.Count == 0) return null;
        if (nodes!.Count == 1) return nodes[0];
        if (trackerBase is null)
            return SyncRootMergerHelper.GetDifferencesByFileContents(nodes);

        return trackerBase?.GetDifferences(nodes);
    }

    /// <inheritdoc/>
    public async Task<List<XElement>> GetAllNodesAsync(string[] filePaths)
    {
        var nodes = new List<XElement>(filePaths.Length);
        foreach (var file in filePaths)
        {
            if (!FileExists(file)) continue;
            var element = await LoadXElementSafeAsync(file);
            if (element != null)
                nodes.Add(element);
        }

        return nodes;
    }

    /// <inheritdoc/>
    public bool AnyFolderExists(string[] folders)
        => folders.Any(DirectoryExists);

    private static string[] GetFilePaths(string folder, string extension)
        => Directory.GetFiles(folder, $"*.{extension}", SearchOption.AllDirectories);

    private async Task<XElement?> LoadXElementSafeAsync(string file)
    {
        var absPath = GetAbsPath(file);
        if (FileExists(absPath) is false) return null;

        try
        {
            return await LoadXElementAsync(file);
        }
        catch
        {
            return null;
        }
    }
    #endregion

}
