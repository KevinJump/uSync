using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using uSync.BackOffice.Legacy.Migrators;

namespace uSync.BackOffice.Legacy;

/// <summary>
///  legacy usync file detection 
/// </summary>
public interface ISyncLegacyService
{
    /// <summary>
    ///  copy a folder over the current uSync folder, so we can import it.
    /// </summary>
    bool CopyLegacyFolder(string folder);

    /// <summary>
    ///  copy a folder over the current uSync folder, and run the migrators with these ids over the copied files.
    /// </summary>
    Task<bool> CopyLegacyFolderAsync(string folder, IEnumerable<string> migrationIds);

    /// <summary>
    ///  find the migrators that apply to the data types in a legacy folder.
    /// </summary>
    Task<List<SyncLegacyMigrationInfo>> FindMigrationsAsync(string folder);

    /// <summary>
    ///  find any legacy data types in the uSync folder
    /// </summary>
    Task<List<string>> FindLegacyDataTypesAsync(string folder);

    /// <summary>
    ///  adds a .ignore file to the root of the folder, so we don't flag it as a legacy folder again.
    /// </summary>
    Task<bool> IgnoreLegacyFolderAsync(string folder, string message);

    /// <summary>
    ///  find the latest uSync legacy folder.
    /// </summary>
    bool TryGetLatestLegacyFolder([MaybeNullWhen(false)] out string? folder);
}