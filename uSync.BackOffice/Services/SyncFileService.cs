using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Extensions;

using uSync.Core;

namespace uSync.BackOffice.Services
{
    /// <summary>
    ///  putting all file actions in a service, 
    ///  so if we want to abstract later we can.
    /// </summary>
    public class SyncFileService
    {
        private readonly ILogger<SyncFileService> logger;
        private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// Constructor for File service (via DI)
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="hostEnvironment"></param>
        public SyncFileService(ILogger<SyncFileService> logger,
                               IHostEnvironment hostEnvironment)
        {
            this.logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        /// <summary>
        ///  return the absolute path for any given path. 
        /// </summary>
        public string GetAbsPath(string path)
        {
            if (Path.IsPathFullyQualified(path)) return CleanLocalPath(path);
            return CleanLocalPath(_hostEnvironment.MapPathContentRoot(path.TrimStart('/')));
        }

        /// <summary>
        ///  clean up the local path, and full expand any short file names
        /// </summary>
        private string CleanLocalPath(string path)
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
        public FileStream OpenRead(string path)
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
            Directory.CreateDirectory(Path.GetDirectoryName(absTarget));
            File.Copy(absSource, absTarget, true);
        }

        /// <summary>
        ///  create the directory for a given file. 
        /// </summary>
        /// <param name="filePath"></param>
        public void CreateFoldersForFile(string filePath)
        {
            var absPath = Path.GetDirectoryName(GetAbsPath(filePath));
            if (!Directory.Exists(absPath))
                Directory.CreateDirectory(absPath);
        }

        /// <summary>
        ///  Create a directory.
        /// </summary>
        /// <param name="folder"></param>
        public void CreateFolder(string folder)
        {
            var absPath = GetAbsPath(folder);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
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
        /// <param name="allFolders">get all files in all decentant folders</param>
        /// <returns></returns>
        public IEnumerable<string> GetFiles(string folder, string extensions, bool allFolders)
        {
            var localPath = GetAbsPath(folder);
            if (DirectoryExists(localPath))
            {
                return Directory.GetFiles(localPath, extensions, allFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            return Enumerable.Empty<string>();

        }

        /// <summary>
        ///  get a list of child folders in a folder 
        /// </summary>
        public IEnumerable<string> GetDirectories(string folder)
        {
            var localPath = GetAbsPath(folder);
            if (DirectoryExists(localPath))
            {
                return Directory.GetDirectories(localPath);
            }

            return Enumerable.Empty<string>();
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
                    return XElement.Load(stream);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Error while reading in {file} {message}", file, ex.Message);
                throw new Exception($"Error while reading in {file}", ex);
            }
        }

        /// <summary>
        ///  save a stream to disk
        /// </summary>
        public void SaveFile(string filename, Stream stream)
        {
            logger.LogDebug("Saving File: {0}", filename);

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
            logger.LogDebug("Saving File: {0} [{1}]", localFile, content.Length);

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
        public TObject LoadXml<TObject>(string file)
        {
            if (FileExists(file))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(TObject));
                using (var stream = OpenRead(file))
                {
                    var item = (TObject)xmlSerializer.Deserialize(stream);
                    return item;
                }
            }
            return default(TObject);
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


        public void DeleteFolder(string folder, bool safe = false)
        {
            try
            {
                var resolvedFolder = GetAbsPath(folder);
                if (Directory.Exists(resolvedFolder))
                    Directory.Delete(resolvedFolder, true);
            }
            catch(Exception ex)
            {
                // can happen when its locked, question is - do you care?
                logger.LogWarning(ex, "Failed to remove directory {folder}", folder);
                if (!safe) throw; 
            }
        }

        public void CopyFolder(string source, string target)
        {
            var resolvedSource = GetAbsPath(source).TrimEnd(Path.DirectorySeparatorChar); ;
            var resolvedTarget = GetAbsPath(target).TrimEnd(Path.DirectorySeparatorChar);

            if (!Directory.Exists(resolvedSource))
                throw new DirectoryNotFoundException(source);

            Directory.CreateDirectory(resolvedTarget);

            // create all the sub folders 
            var folders = Directory.GetDirectories(resolvedSource, "*", SearchOption.AllDirectories);
            foreach(var folder in folders)
            {
                Directory.CreateDirectory(folder.Replace(resolvedSource, resolvedTarget));
            }

            // copy all the files
            var files = Directory.GetFiles(resolvedSource, "*.*", SearchOption.AllDirectories);
            foreach(var file in files)
            {
                File.Copy(file, file.Replace(resolvedSource, resolvedTarget), true);
            }
               
        }


        // TODO: this doesn't need to be public? 




        /// <summary>
        ///  Locking item for saves. 
        /// </summary>
        public static object _saveLock = new object();

        /// <summary>
        ///  save an object to an XML file representing it.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="file"></param>
        /// <param name="item"></param>
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
        /// <param name="folder"></param>
        /// <returns></returns>
        public List<string> VerifyFolder(string folder, string extension)
        {
            var resolvedFolder = GetAbsPath(folder);
            if (!DirectoryExists(resolvedFolder))
                throw new DirectoryNotFoundException(folder);

            var keys = new Dictionary<Guid, string>();
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
                        var key = node.GetKey();
                        var folderName = Path.GetFileName(Path.GetDirectoryName(file));
                        var filename = Path.GetFileName(file);
                        var filepath = GetShortFileName(file);

                        if (!keys.ContainsKey(key))
                        {
                            keys[key] = filepath;
                        }
                        else
                        {
                            errors.Add($"Clash {filepath} shares an id with {keys[key]}");
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

        string GetShortFileName(string file)
            => $"{Path.DirectorySeparatorChar}{Path.GetFileName(Path.GetDirectoryName(file))}" +
            $"{Path.DirectorySeparatorChar}{Path.GetFileName(file)}";

    }

}
