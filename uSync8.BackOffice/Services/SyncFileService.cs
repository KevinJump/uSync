using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.IO;

namespace uSync8.BackOffice.Services
{
    /// <summary>
    ///  putting all file actions in a service, 
    ///  so if we want to abstract later we can.
    /// </summary>
    public class SyncFileService
    {
        private readonly uSyncBackOfficeSettings globalSettings;
        private readonly string mappedRoot;

        public SyncFileService(uSyncBackOfficeSettings settings)
        {
            this.globalSettings = settings;
            this.mappedRoot = IOHelper.MapPath(globalSettings.rootFolder);
        }


        private string GetAbsPath(string path)
        {
            if (path.StartsWith(mappedRoot)) return path;
            return IOHelper.MapPath(globalSettings.rootFolder + path.TrimStart(new char[] { '/' }));
        }

        public bool FileExists(string path)
        {
            return File.Exists(GetAbsPath(path));
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(GetAbsPath(path));
        }

        public bool RootExists()
        {
            return DirectoryExists("");
        }

        public void DeleteFile(string path)
        {
            File.Delete(GetAbsPath(path));
        }

        public void EnsureFileExists(string path)
        {
            if (!FileExists(path))
                throw new FileNotFoundException("Missing File",  path);
        }

        public Stream OpenRead(string path)
        {
            if (!FileExists(path)) return null;

            var absPath = GetAbsPath(path);
            return File.OpenRead(path);
        }

        public Stream OpenWrite(string path)
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
    }
}
