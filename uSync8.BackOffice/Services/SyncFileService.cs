using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Umbraco.Core.Composing;
using Umbraco.Core.IO;
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

        public SyncFileService()
        {
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
            if (path.StartsWith(mappedRoot)) return path;
            return IOHelper.MapPath(path.TrimStart(new char[] { '/' }));
        }

        public bool FileExists(string path)
        {
            return File.Exists(GetAbsPath(path));
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(GetAbsPath(path));
        }

        public bool RootExists(string path)
        {
            return DirectoryExists(path);
        }

        public void DeleteFile(string path)
        {
            if (FileExists(path))
                File.Delete(GetAbsPath(path));
        }

        public void EnsureFileExists(string path)
        {
            if (!FileExists(path))
                throw new FileNotFoundException("Missing File",  path);
        }

        public FileStream OpenRead(string path)
        {
            if (!FileExists(path)) return null;

            var absPath = GetAbsPath(path);
            return File.OpenRead(absPath);
        }

        public FileStream OpenWrite(string path)
        {
            if (FileExists(path))
                DeleteFile(path);

            CreateFoldersForFile(path);

            var absPath = GetAbsPath(path);
            return File.OpenWrite(absPath);
        }


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

        public IEnumerable<string> GetFiles(string folder, string extensions)
        {
            if (DirectoryExists(folder))
            {
                return Directory.GetFiles(GetAbsPath(folder));
            }

            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetDirectories(string folder)
        {
            if (DirectoryExists(folder))
            {
                return Directory.GetDirectories(GetAbsPath(folder));
            }

            return Enumerable.Empty<string>();
        }

        public XElement LoadXElement(string file)
        {
            EnsureFileExists(file);

            using (var stream = OpenRead(file))
            {
                return XElement.Load(stream);
            }
        }

        public void SaveFile(string filename, Stream stream)
        {
            using(Stream fileStream = OpenWrite(filename))
            {
                stream.CopyTo(fileStream);
                fileStream.Flush();
                fileStream.Close();
            }
        }

        public void SaveFile(string filename, string content)
        {
            
            using (Stream stream = OpenWrite(filename))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(content);
                stream.Write(info, 0, info.Length);
                stream.Flush();
                stream.Dispose();
            }
        }

        public void SaveXElement(XElement node, string filename)
        {
            using (var stream = OpenWrite(filename))
            {
                node.Save(stream);
                stream.Flush();
                stream.Dispose();
            }
        }

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

        public void SaveXml<TObject>(string file, TObject item)
        {
            lock(_saveLock)
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
