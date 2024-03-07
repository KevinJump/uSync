using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace uSync.BackOffice.Legacy;

/// <summary>
///  legacy usync file detection 
/// </summary>
public interface ISyncLegacyService
{
    /// <summary>
    ///  find any legacy data types in the uSync folder
    /// </summary>
    List<string> FindLegacyDataTypes(string folder);

    /// <summary>
    ///  find the latest uSync legacy folder.
    /// </summary>
    bool TryGetLatestLegacyFolder([MaybeNullWhen(false)] out string? folder);
}