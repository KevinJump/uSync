using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

using Umbraco.Core.Composing;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using uSync8.BackOffice.Configuration;

namespace uSync8.BackOffice.Services
{
    /// <summary>
    ///  putting all file actions in a service, 
    ///  so if we want to abstract later we can.
    /// </summary>
    public class SyncFileService
    {
        private uSyncSettings globalSettings;
        private string mappedRoot;
        private readonly IProfilingLogger logger;

        public SyncFileService(IProfilingLogger logger)
        {
            this.logger = logger;
            this.globalSettings = Current.Configs.uSync();
            this.mappedRoot = IOHelper.MapPath(globalSettings.RootFolder);

            uSyncConfig.Reloaded += BackOfficeConfig_Reloaded;
        }

        private void BackOfficeConfig_Reloaded(uSyncSettings settings)
        {
            this.globalSettings = Current.Configs.uSync();
            this.mappedRoot = IOHelper.MapPath(globalSettings.RootFolder);
        }

        public string GetAbsPath(string path)
        {
            if (path.StartsWith(mappedRoot)) return path.Replace('/', '\\');
            return IOHelper.MapPath(path.TrimStart(new char[] { '/' }));
        }

        public bool FileExists(string path)
            => File.Exists(GetAbsPath(path));


        /// <summary>
        ///  compare two file paths, and tell us if they match 
        /// </summary>
        public bool PathMatches(string a, string b)
            => GetAbsPath(a).Equals(GetAbsPath(b), StringComparison.InvariantCultureIgnoreCase);

        public bool DirectoryExists(string path)
            => Directory.Exists(GetAbsPath(path));
    
        public bool RootExists(string path)
            => DirectoryExists(path);

        public void DeleteFile(string path)
        {
            if (FileExists(path))
                File.Delete(GetAbsPath(path));
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
            if (!FileExists(path)) return null;

            var absPath = GetAbsPath(path);
            return File.OpenRead(absPath);
        }

        /// <summary>
        ///  Open a file stream for writing a file
        /// </summary>
        public FileStream OpenWrite(string path)
        {
            if (FileExists(path))
                DeleteFile(path);

            CreateFoldersForFile(path);

            var absPath = GetAbsPath(path);
            return File.OpenWrite(absPath);
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
        {
            if (DirectoryExists(folder))
            {
                return Directory.GetFiles(GetAbsPath(folder), extensions);
            }

            return Enumerable.Empty<string>();
        }

        /// <summary>
        ///  get a list of child folders in a folder 
        /// </summary>
        public IEnumerable<string> GetDirectories(string folder)
        {
            if (DirectoryExists(folder))
            {
                return Directory.GetDirectories(GetAbsPath(folder));
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
            catch(Exception ex)
            {
                logger.Warn<SyncFileService>("Error while reading in {file} {message}", file, ex.Message);
                throw new Exception($"Error while reading in {file}", ex);
            }
        }

        /// <summary>
        ///  save a stream to disk
        /// </summary>
        public void SaveFile(string filename, Stream stream)
        {
            logger.Debug<SyncFileService>("Saving File: {0}", filename);

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
            logger.Debug<SyncFileService>("Saving File: {0} [{1}]", filename, content.Length);

            using (Stream stream = OpenWrite(filename))
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
            using (var stream = OpenWrite(filename))
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
    }
}
