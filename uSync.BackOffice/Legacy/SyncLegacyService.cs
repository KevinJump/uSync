using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using uSync.BackOffice.Services;
using uSync.Core;

namespace uSync.BackOffice.Legacy;

/// <summary>
///  checks for legacy datatypes,and helps convert them.
/// </summary>
internal class SyncLegacyService : ISyncLegacyService
{
    private int _majorVersion = uSync.Version.Major;

    private static readonly Dictionary<string, string> _legacyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { SyncLegacyTypes.NestedContent, "Nested Content" },
        { SyncLegacyTypes.OurNestedContent, "Nested Content (Community version)" },
        { SyncLegacyTypes.Grid, "Grid" },
        // { SyncLegacyTypes.MediaPicker, "Media Picker (Legacy)" },
        //{ SyncLegacyTypes.MediaPicker2, "Media Picker (2) (Legacy)" },
        { SyncLegacyTypes.MultipleMediaPicker, "Multiple Media Picker (Legacy)" }
    };

    private readonly SyncFileService _syncFileService;

    public SyncLegacyService(SyncFileService syncFileService)
    {
        _syncFileService = syncFileService;
    }


    public bool TryGetLatestLegacyFolder([MaybeNullWhen(false)] out string? folder)
    {
        folder = null;

        for(int n = _majorVersion-1; n > 8; n--)
        {
            var legacyFolder = $"~/uSync/v{n}";
            if (_syncFileService.DirectoryExists(legacyFolder))
            {
                if (_syncFileService.FileExists(Path.Combine(legacyFolder, ".ignore")) is true)
					continue;

                folder = legacyFolder;
                return true;
            }
        }

        return false;
    }

    public bool IgnoreLegacyFolder(string folder, string message)
	{
		if (_syncFileService.DirectoryExists(folder) is false)
			return false;

		_syncFileService.SaveFile(Path.Combine(folder, ".ignore"),
            $"{message}\r\nDelete this file for this folder to be detected as legacy again");
		return true;
	}

    public bool CopyLegacyFolder(string folder)
    {
        var latestPath = $"~/uSync/v{_majorVersion}";
        var backupPath = $"~/uSync/v{_majorVersion}-backup";

        if (_syncFileService.DirectoryExists(folder) is false)
			return false;

        if (_syncFileService.DirectoryExists(latestPath))
        {
            _syncFileService.CopyFolder(latestPath, backupPath);
            _syncFileService.DeleteFolder(latestPath);
		}

        _syncFileService.CreateFolder(latestPath);

        _syncFileService.CopyFolder(folder, latestPath);
        return true;
    }

	public List<string> FindLegacyDataTypes(string folder)
    {

        var dataTypesFolder = Path.Combine(folder, "DataTypes");
        if (_syncFileService.DirectoryExists(dataTypesFolder) is false)
            return [];

        var discoveredLegacyTypes = new List<string>();

        foreach (var file in _syncFileService.GetFiles(dataTypesFolder, "*.config"))
        {
            var node = _syncFileService.LoadXElement(file);
            if (node.Name.LocalName.Equals(Core.uSyncConstants.Serialization.DataType) is false)
                continue;

            var type = node.Element(Core.uSyncConstants.Xml.Info)
                    ?.Element("EditorAlias").ValueOrDefault(string.Empty);
            if (string.IsNullOrEmpty(type)) continue;

            if (_legacyTypes.TryGetValue(type, out string? value))
            {
                discoveredLegacyTypes.Add(value);
            }
        }

        return discoveredLegacyTypes;
    }
}
