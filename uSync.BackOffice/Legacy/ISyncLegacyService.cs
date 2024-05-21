using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
	///  find any legacy data types in the uSync folder
	/// </summary>
	List<string> FindLegacyDataTypes(string folder);

	/// <summary>
	///  adds a .ignore file to the root of the folder, so we don't flag it as a legacy folder again.
	/// </summary>
	bool IgnoreLegacyFolder(string folder, string message);

	/// <summary>
	///  find the latest uSync legacy folder.
	/// </summary>
	bool TryGetLatestLegacyFolder([MaybeNullWhen(false)] out string? folder);
}