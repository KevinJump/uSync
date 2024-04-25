using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

using Examine;

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
public class SyncFileService
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

    /// <summary>
    ///  return the absolute path for any given path. 
    /// </summary>
    public string GetAbsPath(string path)
    {
        if (Path.IsPathFullyQualified(path)) return CleanLocalPath(path);
        return CleanLocalPath(_hostEnvironment.MapPathContentRoot(path.TrimStart(_trimChars)));
    }

    /// <summary>
    ///  Works out the relative path of a file to the site. 
    /// </summary>
    /// <remarks>
    ///  if the path is outside of the site root, then we return the whole path.
    /// </remarks>
    public string GetSiteRelativePath(string path)
    {
        if (Path.IsPathFullyQualified(path) && path.StartsWith(_hostEnvironment.ContentRootPath))
            return path.Substring(_hostEnvironment.ContentRootPath.Length).TrimStart(_trimChars);
        return path;
    }

    /// <summary>
    ///  clean up the local path, and full expand any short file names
    /// </summary>
    private static string CleanLocalPath(string path)
        => Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

    /// <summary>
    ///  does a file exist 
    /// </summary>
    public bool FileExists(string path)
        => File.Exists(GetAbsPath(path));


    /// <summary>
    ///  compare two file paths, and tell us if they match 
    /// </summary>
    public bool PathMatches(string a, string b)
        => GetAbsPath(a).Equals(GetAbsPath(b), StringComparison.InvariantCultureIgnoreCase);

    /// <summary>
    ///  does a directory exist
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool DirectoryExists(string path)
        => Directory.Exists(GetAbsPath(path));

    /// <summary>
    ///  checks and tells you if a folder exists and has sub folders.
    /// </summary>
    /// <remarks>
    ///  we use this to confirm that a uSync folder has something init.
    /// </remarks>
    public bool DirectoryHasChildren(string path)
    {
        var fullPath = GetAbsPath(path);
        if (Directory.Exists(fullPath) is false) return false;
        return Directory.GetDirectories(fullPath).Length > 0;
    }

    /// <summary>
    ///  dies the root path exist. 
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool RootExists(string path)
        => DirectoryExists(path);

    /// <summary>
    ///  remove a file from disk.
    /// </summary>
    /// <param name="path"></param>
    public void DeleteFile(string path)
    {
        var localPath = GetAbsPath(path);
        if (FileExists(localPath))
            File.Delete(localPath);
    }

    /// <summary>
    ///  Check if a file exists throw an exception if it doesn't 
    /// </summary>
    public void EnsureFileExists(string path)
    {
        if (!FileExists(path))
            throw new FileNotFoundException("Missing File", path);
    }

    /// <summary>
    ///  open a file stream for reading a file 
    /// </summary>
    public FileStream? OpenRead(string path)
    {
        var localPath = GetAbsPath(path);

        if (!FileExists(localPath)) return null;
        return File.OpenRead(localPath);
    }

    /// <summary>
    ///  Open a file stream for writing a file
    /// </summary>
    public FileStream OpenWrite(string path)
    {
        var localPath = GetAbsPath(path);

        if (FileExists(localPath))
            DeleteFile(localPath);

        CreateFoldersForFile(path);
        return File.OpenWrite(localPath);
    }

    /// <summary>
    ///  copy a file from one location to another - creating the directory if it is missing
    /// </summary>
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

    /// <summary>
    ///  create the directory for a given file. 
    /// </summary>
    /// <param name="filePath"></param>
    public void CreateFoldersForFile(string filePath)
    {
        var absPath = Path.GetDirectoryName(GetAbsPath(filePath));
        if (string.IsNullOrEmpty(absPath)) return;

        if (Directory.Exists(absPath) is false)
            Directory.CreateDirectory(absPath);
    }

    /// <summary>
    ///  Create a directory.
    /// </summary>
    /// <param name="folder"></param>
    public void CreateFolder(string folder)
    {
        var absPath = GetAbsPath(folder);
        if (!Directory.Exists(absPath))
            Directory.CreateDirectory(absPath);
    }

    /// <summary>
    ///  remove a folder and all its contents
    /// </summary>
    public void CleanFolder(string folder)
    {
        var absPath = GetAbsPath(folder);

        if (Directory.Exists(absPath))
            Directory.Delete(absPath, true);
    }

    /// <summary>
    ///  Get a list of files from a folder. 
    /// </summary>
    public IEnumerable<string> GetFiles(string folder, string extensions)
        => GetFiles(folder, extensions, false);

    /// <summary>
    ///  get all the files in a folder, 
    /// </summary>
    /// <param name="folder">path to the folder</param>
    /// <param name="extensions">list of extensions (filter)</param>
    /// <param name="allFolders">get all files in all descendant folders</param>
    /// <returns></returns>
    public IEnumerable<string> GetFiles(string folder, string extensions, bool allFolders)
    {
        var localPath = GetAbsPath(folder);

        if (!DirectoryExists(localPath)) return [];

        return Directory.GetFiles(localPath, extensions, allFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
    }

    /// <summary>
    ///  get a list of child folders in a folder 
    /// </summary>
    public IEnumerable<string> GetDirectories(string folder)
    {
        var localPath = GetAbsPath(folder);
        if (!DirectoryExists(localPath)) return [];
        
        return Directory.GetDirectories(localPath);
       
    }

    /// <summary>
    ///  load a file into a XElement object.
    /// </summary>
    public XElement LoadXElement(string file)
    {
        EnsureFileExists(file);

        try
        {
            using (var stream = OpenRead(file))
            {
                if (stream is null)
                    throw new FileNotFoundException($"Cannot create stream for {file}"); ;

                return XElement.Load(stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error while reading in {file} {message}", file, ex.Message);
            throw new Exception($"Error while reading in {file}", ex);
        }
    }

    /// <summary>
    ///  save a stream to disk
    /// </summary>
    public void SaveFile(string filename, Stream stream)
    {
        _logger.LogDebug("Saving File: {file}", filename);

        using (Stream fileStream = OpenWrite(filename))
        {
            stream.CopyTo(fileStream);
            fileStream.Flush();
            fileStream.Close();
        }
    }

    /// <summary>
    ///  save a string to disk
    /// </summary>
    public void SaveFile(string filename, string content)
    {
        var localFile = GetAbsPath(filename);
        _logger.LogDebug("Saving File: {local} [{length}]", localFile, content.Length);

        using (Stream stream = OpenWrite(localFile))
        {
            byte[] info = new UTF8Encoding(true).GetBytes(content);
            stream.Write(info, 0, info.Length);
            stream.Flush();
            stream.Dispose();
        }
    }

    /// <summary>
    ///  Save an XML Element to disk
    /// </summary>
    public void SaveXElement(XElement node, string filename)
    {
        var localPath = GetAbsPath(filename);
        using (var stream = OpenWrite(localPath))
        {
            node.Save(stream);
            stream.Flush();
            stream.Dispose();
        }
    }

    /// <summary>
    ///  Load an object from XML representation on disk.
    /// </summary>
    public TObject? LoadXml<TObject>(string file)
    {
        if (FileExists(file))
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(TObject));
            using (var stream = OpenRead(file))
            {
                if (stream is null) return default;

                TObject? item = (TObject?)xmlSerializer.Deserialize(stream);
                return item;
            }
        }
        return default;
    }

    /// <summary>
    ///  load the contents of a file into a string
    /// </summary>
    public string LoadContent(string file)
    {
        if (FileExists(file))
        {
            var absPath = this.GetAbsPath(file);
            return File.ReadAllText(absPath);
        }

        return string.Empty;
    }

    /// <summary>
    /// Remove a folder from disk
    /// </summary>
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

    /// <summary>
    ///  copy the contents of a folder 
    /// </summary>
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


    /// <summary>
    ///  Locking item for saves. 
    /// </summary>
    private static object _saveLock = new();

    /// <summary>
    ///  save an object to an XML file representing it.
    /// </summary>
    public void SaveXml<TObject>(string file, TObject item)
    {
        lock (_saveLock)
        {
            if (FileExists(file))
                DeleteFile(file);

            var xmlSerializer = new XmlSerializer(typeof(TObject));
            using (var stream = OpenWrite(file))
            {
                xmlSerializer.Serialize(stream, item);
            }
        }
    }

    /// <summary>
    ///  run some basic sanity checks on a folder to see if it looks like a good 
    ///  set of uSync files ? 
    /// </summary>
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

    /// <summary>
    ///  Merge a number of uSync folders into a single 'usync source'
    /// </summary>
    /// <remarks>
    ///  this is the core of the "roots" functionality. folders are 
    ///  merged upwards (so the last folder will win)
    ///  
    ///  custom merging can be achieved using additional methods on
    ///  the change trackers. 
    ///  
    ///  the doctype tracker merges properties so you can have 
    ///  property level root values for doctypes. 
    /// </remarks>
    public IEnumerable<OrderedNodeInfo> MergeFolders(string[] folders, string extension, ISyncTrackerBase? trackerBase)
    {
        var elements = new Dictionary<string, OrderedNodeInfo>();

        foreach (var folder in folders)
        {
            var absPath = GetAbsPath(folder);

            if (DirectoryExists(absPath) is false) continue;

            var items = GetFolderItems(absPath, extension);

            var localKeys = new List<Guid>();

            foreach (var item in items)
            {
				var itemKey = item.Value.Node.GetKey();
				if (localKeys.Contains(itemKey))
				{
					throw new Exception($"Duplicate: Item key {itemKey} already exists for {item.Key} - run uSync Health check for more info.");
				}
				
                localKeys.Add(itemKey);

                if (trackerBase is not null && elements.TryGetValue(item.Key, out var value))
                {
                    // merge these files.
                    item.Value.SetNode(MergeNodes(value.Node, item.Value.Node, trackerBase));
                }

                elements[item.Key] = item.Value;
            }
        }

        return elements.Values;
    }

	/// <summary>
	///  merge the files into a single XElement that can be imported as if it was on disk.
	/// </summary>
	public XElement? MergeFiles(string[] filenames, ISyncTrackerBase? trackerBase)
	{
		if (filenames.Length == 0) return null;
		var latest = LoadXElementSafe(filenames[0]);
		if (filenames.Length == 1 || latest is null) return latest;

		for (var n = 1; n < filenames.Length; n++)
		{
			var node = LoadXElementSafe(filenames[n]);
			if (node is null) continue;
			latest = MergeNodes(latest, node, trackerBase);
		}
		return latest;
	}

	private XElement MergeNodes(XElement source, XElement target, ISyncTrackerBase? trackerBase)
		=> trackerBase is null ? target : trackerBase.MergeFiles(source, target) ?? target;


	private IEnumerable<KeyValuePair<string, OrderedNodeInfo>> GetFolderItems(string folder, string extension)
    {
        foreach (var file in GetFilePaths(folder, extension))
        {
            var element = LoadXElementSafe(file);
            if (element != null)
            {
                var path = file.Substring(folder.Length);

                yield return new KeyValuePair<string, OrderedNodeInfo>(
                    key: path,
                    value: new OrderedNodeInfo(
                        filename: file,
                        node: element,
                        level: (element.GetLevel() * 1000) + element.GetItemSortOrder(),
                        path: path,
                        isRoot: true));
            }
        }
    }

    /// <summary>
    ///  will load the most relevant version of a file. 
    /// </summary>
    public XElement? GetNearestNode(string filePath, string[] folders)
    {
        foreach (var folder in folders.Reverse())
        {
            var path = Path.Combine(folder, filePath);
            if (FileExists(path))
                return LoadXElementSafe(path);
        }

        return null;
    }

    /// <summary>
    ///  get a XML representation of the differences between two files
    /// </summary>
    /// <remarks>
    ///  the default merger returns the whole xml as the difference. 
    /// </remarks>
    public XElement? GetDifferences(List<XElement> nodes, ISyncTrackerBase? trackerBase)
    {
        if (nodes is null || nodes?.Count == 0) return null;
        if (nodes!.Count == 1) return nodes[0];
        if (trackerBase is null)
            return SyncRootMergerHelper.GetDifferencesByFileContents(nodes);

        return trackerBase?.GetDifferences(nodes);
    }

    /// <summary>
    ///  get all xml elements that represent this item across
    ///  all folders. 
    /// </summary>
    public List<XElement> GetAllNodes(string[] filePaths)
    {
        var nodes = new List<XElement>(filePaths.Length);
        foreach (var file in filePaths)
        {
            if (!FileExists(file)) continue;
            var element = LoadXElementSafe(file);
            if (element != null)
                nodes.Add(element);
        }

        return nodes;
    }

    /// <summary>
    /// checks a list of folders to see if any of them exists
    /// </summary>
    /// <returns>true if any but the last folder exists</returns>
    public bool AnyFolderExists(string[] folders)
        => folders.Any(DirectoryExists);

    private static string[] GetFilePaths(string folder, string extension)
        => Directory.GetFiles(folder, $"*.{extension}", SearchOption.AllDirectories);

    private XElement? LoadXElementSafe(string file)
    {
        var absPath = GetAbsPath(file);
        if (FileExists(absPath) is false) return null;

        try
        {
            return XElement.Load(absPath);
        }
        catch
        {
            return null;
        }
    }

    #endregion

}
