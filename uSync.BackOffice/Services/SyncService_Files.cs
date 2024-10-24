using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace uSync.BackOffice;

public partial class SyncService
{
    /// <summary>
    ///  Zip up the contents of a folder
    /// </summary>
    /// <param name="folder">Path of folder to compress</param>
    /// <returns>Stream of zip file for folder</returns>
    public MemoryStream CompressFolder(string folder)
    {
        var fullPath = _syncFileService.GetAbsPath(folder);

        if (!_syncFileService.DirectoryExists(fullPath))
            throw new DirectoryNotFoundException(fullPath);

        var files = _syncFileService.GetFiles(fullPath, "*.*", true);

        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var file in files)
            {
                var relativePath = GetRelativePath(fullPath, file)
                    .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                archive.CreateEntryFromFile(file, relativePath);
            }
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    /// <summary>
    ///  unzip a folder
    /// </summary>
    public void DeCompressFile(string zipArchive, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("No path");

        if (zipArchive == null || !_syncFileService.FileExists(zipArchive))
            throw new ArgumentException("missing zip");

        var resolvedTarget = _syncFileService.GetAbsPath(target);

        var fullTarget = Path.GetFullPath(resolvedTarget);

        using (var zip = ZipFile.OpenRead(zipArchive))
        {
            if (!zip.Entries.Any(x => x.FullName.EndsWith(_uSyncConfig.Settings.DefaultExtension)))
                throw new Exception("contains no uSync files");

            foreach (var entry in zip.Entries)
            {
                // things that might be folders. 
                if (entry.Length == 0) continue;

                var filePath = GetOSDependentPath(entry.FullName);

                var destination = Path.GetFullPath(Path.Combine(resolvedTarget, filePath));
                if (!destination.StartsWith(fullTarget))
                    throw new InvalidOperationException("Invalid file path");

                var destinationFolder = Path.GetDirectoryName(destination);

                if (destinationFolder is not null && Directory.Exists(destinationFolder) is false)
                    Directory.CreateDirectory(destinationFolder);

                entry.ExtractToFile(destination, true);
            }
        }
    }

    /// <summary>
    ///  replaces files in a folder with those from source.
    /// </summary>
    public void ReplaceFiles(string source, string target, bool clean)
    {
        if (clean)
            _syncFileService.DeleteFolder(target);

        _syncFileService.CopyFolder(source, target);
    }


    private static string GetRelativePath(string root, string file)
    {
        var cleanRoot = CleanPathForZip(root);
        var cleanFile = CleanPathForZip(file);

        if (cleanFile.Length <= cleanRoot.Length || !cleanFile.StartsWith(cleanRoot))
            throw new ArgumentException($"Mismatch {root} is not parent of {file}");

        return cleanFile.Substring(cleanRoot.Length).TrimStart(Path.DirectorySeparatorChar);
    }

    private static string GetOSDependentPath(string file)
        => file.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Trim(Path.DirectorySeparatorChar);

    private static string CleanPathForZip(string path)
        => Path.GetFullPath(
            path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))
            .TrimEnd(Path.DirectorySeparatorChar);
}
