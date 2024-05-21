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
    private static readonly string[] _legacyFolders = [
        "~/uSync/v13",
        "~/uSync/v12",
        "~/uSync/v11",
        "~/uSync/v10",
        "~/uSync/v9"
    ];

    private static readonly Dictionary<string, string> _legacyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { SyncLegacyTypes.NestedContent, "Nested Content" },
        { SyncLegacyTypes.OurNestedContent, "Nested Content (Community version)" },
        { SyncLegacyTypes.Grid, "Grid" },
        { SyncLegacyTypes.MediaPicker, "Media Picker" }
    };

    private readonly SyncFileService _syncFileService;

    public SyncLegacyService(SyncFileService syncFileService)
    {
        _syncFileService = syncFileService;
    }


    public bool TryGetLatestLegacyFolder([MaybeNullWhen(false)] out string? folder)
    {
        folder = null;

        foreach (var legacyFolder in _legacyFolders)
        {
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
        if (_syncFileService.DirectoryExists(folder) is false)
			return false;

        if (_syncFileService.DirectoryExists("~/uSync/v14"))
        {
            _syncFileService.CopyFolder("~/uSync/v14", "~/uSync/v14-backup");
            _syncFileService.DeleteFolder("~/uSync/v14");
		}

        _syncFileService.CreateFolder("~/uSync/v14");

		_syncFileService.CopyFolder(folder, "~/uSync/v14");
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
