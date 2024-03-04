using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace uSync.BackOffice.Legacy;

public interface ISyncLegacyService
{
    List<string> FindLegacyDataTypes(string folder);
    bool TryGetLatestLegacyFolder([MaybeNullWhen(false)] out string? folder);
}