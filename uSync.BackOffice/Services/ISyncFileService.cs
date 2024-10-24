using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

using uSync.Core.Tracking;

namespace uSync.BackOffice.Services;

/// <summary>
///  provides a bit of abstraction away from the file system for uSync jobs.
/// </summary>
/// <remarks>
///  we are not using a IFileSystem (yet) as we actually use this service to 
///  write to a few locations (e.g temp) as well as the uSync folder. 
///  
///  all uSync code should use this service to access the disk, bypassing it
///  makes it harder to maintain (and migrate to a IFileSystem if we ever do)
/// </remarks>
public interface ISyncFileService
{
    /// <summary>
    ///  check if any of the folders in the list exist
    /// </summary>
    bool AnyFolderExists(string[] folders);

    /// <summary>
    ///  remove all the files from a folder 
    /// </summary>
    void CleanFolder(string folder);

    /// <summary>
    ///  copy a file from source to target (creating folders as needed)
    /// </summary>
    void CopyFile(string source, string target);

    /// <summary>
    ///  copy the contents of a folder from a to b
    /// </summary>
    void CopyFolder(string source, string target);

    /// <summary>
    ///  create a folder (if it doesn't exist)
    /// </summary>
    void CreateFolder(string folder);

    /// <summary>
    ///  create the folders required for a file to exist
    /// </summary>
    void CreateFoldersForFile(string filePath);

    /// <summary>
    ///  delete a file 
    /// </summary>
    void DeleteFile(string path);

    /// <summary>
    ///  delete a folder (and all its contents)
    /// </summary>
    /// <param name="safe">don't throw an exception if this fails</param>
    void DeleteFolder(string folder, bool safe = false);

    /// <summary>
    ///  does the specified directory exist
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    ///  does the directory contain any sub folders 
    /// </summary>
    bool DirectoryHasChildren(string path);

    /// <summary>
    ///  check if a file exists - throw an exception if it doesn't
    /// </summary>
    void EnsureFileExists(string path);

    /// <summary>
    ///  does a file exist 
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    ///  return the absolute path for a file on the website 
    /// </summary>
    string GetAbsPath(string path);

    /// <summary>
    ///  return all the XElement nodes from a list of files
    /// </summary>
    Task<List<XElement>> GetAllNodesAsync(string[] filePaths);

    /// <summary>
    ///  get a XML representation of the differences between two files
    /// </summary>
    XElement? GetDifferences(List<XElement> nodes, ISyncTrackerBase? trackerBase);

    /// <summary>
    ///  list the folders inside a folder
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    IEnumerable<string> GetDirectories(string folder);

    /// <summary>
    ///  get all the files of extension type in a folder 
    /// </summary>
    IEnumerable<string> GetFiles(string folder, string extensions);

    /// <summary>
    ///  get all the files of extension type in a folder and sub folders 
    /// </summary>
    IEnumerable<string> GetFiles(string folder, string extensions, bool allFolders);

    /// <summary>
    ///  will load the most relevant version of a file. (e.g the first one in the list of folders)
    /// </summary>
    Task<XElement?> GetNearestNodeAsync(string filePath, string[] folders);

    /// <summary>
    ///  get the path relative to the site root. 
    /// </summary>
    string GetSiteRelativePath(string path);

    /// <summary>
    ///  load the content of a file as a string
    /// </summary>
    Task<string> LoadContentAsync(string file);

    /// <summary>
    ///  load the contents of a file into an XElement
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<XElement> LoadXElementAsync(string file);

    /// <summary>
    ///  merge a list of files into a single XElement
    /// </summary>
    /// <remarks>
    ///  depending on the tracker this can do clever things like merge bits of doctypes together.
    /// </remarks>
    Task<XElement?> MergeFilesAsync(string[] filenames, ISyncTrackerBase? trackerBase);

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
    Task<IEnumerable<OrderedNodeInfo>> MergeFoldersAsync(string[] folders, string extension, ISyncTrackerBase? trackerBase);

 
    /// <summary>
    ///  checks to see if the path of a and b are in fact the same (when resolved to the site)
    /// </summary>
    bool PathMatches(string a, string b);

    /// <summary>
    ///  save a stream to disk
    /// </summary>
    Task SaveFileAsync(string filename, Stream stream);

    /// <summary>
    ///  save a string to disk
    /// </summary>
    Task SaveFileAsync(string filename, string content);

    /// <summary>
    ///  save an XElement to disk
    /// </summary>
    Task SaveXElementAsync(XElement node, string filename);

    /// <summary>
    ///  run some basic checks on a folder to see if it looks ok. 
    /// </summary>
    List<string> VerifyFolder(string folder, string extension);
}