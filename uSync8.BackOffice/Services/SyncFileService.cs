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

        public SyncFileService(uSyncBackOfficeSettings settings)
        {
            this.globalSettings = settings;
        }


        private string GetAbsPath(string path)
        {
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
    }
}
