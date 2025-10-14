using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice;

public partial class SyncService
{
    /// <inheritdoc/>>
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

    /// <inheritdoc/>>
    public void DeCompressFile(Stream zipArchive, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("No path");

        if (zipArchive == null)
            throw new ArgumentException("missing zip");

        var resolvedTarget = _syncFileService.GetAbsPath(target);

        var fullTarget = Path.GetFullPath(resolvedTarget);

        using (var zip = new ZipArchive(zipArchive, ZipArchiveMode.Read))
        {
            if (zip == null)
                throw new Exception("Invalid zip file");

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

    /// <inheritdoc/>>
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

    /// <inheritdoc />
    public async Task<int> MergeExportFolder(string[] paths, IEnumerable<HandlerConfigPair> handlers, bool clean)
    {
        var totalMerged = 0;
        var root = _uSyncConfig.GetWorkingFolder();

        foreach (var handler in handlers)
        {
            var serializerType = handler.Handler.GetSerializeType();
            var baseTracker = handler.Handler.GetBaseTracker();
            if (serializerType is null || baseTracker is null)
            {
                _logger.LogWarning("Handler {Handler} does not support file merging", handler.Handler.Alias);
                continue;
            }

            var folders = paths.Select(x => Path.Combine(x, handler.Handler.DefaultFolder)).ToArray();
            var targetFileName = Path.Combine(_uSyncConfig.Settings.ProductionFolder,
                handler.Handler.DefaultFolder + "." + _uSyncConfig.Settings.DefaultExtension);

            totalMerged += await _syncFileService.MakeSingleExportFromFolders(folders, serializerType, baseTracker, targetFileName, _uSyncConfig.Settings.DefaultExtension);
        }

        return totalMerged;
    }
}
