using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using uSync.BackOffice.Legacy.Migrators;

namespace uSync.BackOffice.Legacy;

internal partial class SyncLegacyService
{
    private const string _dataTypesFolder = "DataTypes";

    /// <inheritdoc/>
    public async Task<List<SyncLegacyMigrationInfo>> FindMigrationsAsync(string folder)
    {
        var dataTypes = await LoadDataTypesAsync(folder);
        var version = GetFolderMajorVersion(folder);

        var results = new List<SyncLegacyMigrationInfo>();

        foreach (var migrator in _migrators)
        {
            var count = dataTypes.Count(x => migrator.AppliesTo(x.Node, version));
            if (count > 0)
                results.Add(new SyncLegacyMigrationInfo(migrator.Id, migrator.Name, migrator.Description, migrator.Notice, count));
        }

        return results;
    }

    private async Task<List<(string File, XElement Node)>> LoadDataTypesAsync(string folder)
    {
        var path = Path.Combine(folder, _dataTypesFolder);
        if (_syncFileService.DirectoryExists(path) is false)
            return [];

        var results = new List<(string, XElement)>();
        foreach (var file in _syncFileService.GetFiles(path, "*.config"))
        {
            var node = await _syncFileService.LoadXElementAsync(file);
            if (node.Name.LocalName.Equals(Core.uSyncConstants.Serialization.DataType))
                results.Add((file, node));
        }

        return results;
    }

    /// <summary>
    ///  the major version of uSync that wrote the folder.
    /// </summary>
    /// <remarks>
    ///  read from the usync.config in the folder, or when that is missing the folder name (v18).
    /// </remarks>
    private int GetFolderMajorVersion(string folder)
    {
        try
        {
            var versionFile = Path.Combine(_syncFileService.GetAbsPath(folder), "usync.config");
            if (File.Exists(versionFile))
            {
                var version = XElement.Load(versionFile).Attribute("version")?.Value;
                if (System.Version.TryParse(version, out var parsed))
                    return parsed.Major;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read the uSync version from {folder}", folder);
        }

        var match = FolderVersionRegex().Match(folder.TrimEnd('/', '\\'));
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    [GeneratedRegex(@"v(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex FolderVersionRegex();

    /// <summary>
    ///  run the migrators over a folder of copied files.
    /// </summary>
    private async Task RunMigratorsAsync(string folder, int folderVersion, List<ISyncLegacyMigrator> migrators)
    {
        var migrated = new Dictionary<Guid, SyncLegacyDataTypeMigration>();

        foreach (var (file, node) in await LoadDataTypesAsync(folder))
        {
            var migrator = migrators.FirstOrDefault(x => x.AppliesTo(node, folderVersion));
            if (migrator is null) continue;

            var result = migrator.MigrateDataType(node);
            if (result is null) continue;

            await _syncFileService.SaveXElementAsync(node, file);
            _logger.LogInformation("Legacy migration {migrator}: {from} to {to} in {file}",
                migrator.Id, result.OriginalEditorAlias, result.NewEditorAlias, Path.GetFileName(file));

            if (result.DataTypeKey != Guid.Empty)
                migrated[result.DataTypeKey] = result;
        }

        if (migrated.Values.Any(x => x.Migrator.MigratesValues))
            await MigrateValuesAsync(folder, migrated);
    }

    private async Task MigrateValuesAsync(string folder, Dictionary<Guid, SyncLegacyDataTypeMigration> migrated)
    {
        var files = _syncFileService.GetFiles(folder, "*.config", true).ToList();

        // 1. the properties of each type, (and the types it is composed from).
        var types = new Dictionary<string, (Dictionary<string, Guid> Properties, List<string> Compositions)>(StringComparer.OrdinalIgnoreCase);

        var valueFiles = new List<(string File, XElement Node)>();

        foreach (var file in files)
        {
            XElement node;
            try { node = await _syncFileService.LoadXElementAsync(file); }
            catch (Exception) { continue; }

            var properties = node.Element("GenericProperties");
            if (properties is not null)
            {
                var alias = node.Attribute("Alias")?.Value ?? node.Element("Info")?.Element("Alias")?.Value;
                if (string.IsNullOrEmpty(alias)) continue;

                var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in properties.Elements("GenericProperty"))
                {
                    var propertyAlias = property.Element("Alias")?.Value;
                    if (string.IsNullOrEmpty(propertyAlias)) continue;
                    if (Guid.TryParse(property.Element("Definition")?.Value, out var definition))
                        map[propertyAlias] = definition;
                }

                var compositions = node.Element("Info")?.Element("Compositions")?.Elements("Composition")
                    .Select(x => x.Value).ToList() ?? [];

                types[alias] = (map, compositions);
            }
            else if (node.Element("Info")?.Element("ContentType") is not null && node.Element("Properties") is not null)
            {
                valueFiles.Add((file, node));
            }
        }

        // 2. rewrite the values.
        foreach (var (file, node) in valueFiles)
        {
            var contentType = node.Element("Info")!.Element("ContentType")!.Value;
            var changed = false;

            foreach (var property in node.Element("Properties")!.Elements())
            {
                if (TryGetDataTypeKey(types, contentType, property.Name.LocalName, [], out var key) is false) continue;
                if (migrated.TryGetValue(key, out var migration) is false || migration.Migrator.MigratesValues is false) continue;

                foreach (var valueNode in property.Elements("Value"))
                {
                    var newValue = migration.Migrator.MigrateValue(valueNode.Value, migration);
                    if (newValue == valueNode.Value) continue;

                    valueNode.ReplaceNodes(new XCData(newValue));
                    changed = true;
                }
            }

            if (changed)
                await _syncFileService.SaveXElementAsync(node, file);
        }
    }

    private static bool TryGetDataTypeKey(
        Dictionary<string, (Dictionary<string, Guid> Properties, List<string> Compositions)> types,
        string typeAlias, string propertyAlias, HashSet<string> visited, out Guid key)
    {
        key = Guid.Empty;
        if (visited.Add(typeAlias) is false) return false;
        if (types.TryGetValue(typeAlias, out var type) is false) return false;

        if (type.Properties.TryGetValue(propertyAlias, out key)) return true;

        foreach (var composition in type.Compositions)
        {
            if (TryGetDataTypeKey(types, composition, propertyAlias, visited, out key))
                return true;
        }

        return false;
    }
}
