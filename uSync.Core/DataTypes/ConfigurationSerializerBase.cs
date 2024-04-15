using System.Collections.Immutable;

using uSync.Core.Extensions;

namespace uSync.Core.DataTypes;

public abstract class ConfigurationSerializerBase
{
    public virtual IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
          => configuration;

    public virtual IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => configuration;

	/// <summary>
	///  renames properties that might exist in a json string (if it is one).
	/// </summary>
	/// <remarks>
	///  will check if the string is JSON if its not, we return the source 
	/// </remarks>
	protected static IDictionary<string, object> MigratePropertyNames(IDictionary<string, object> source, Dictionary<string, string> names, bool sort = true)
	{
		foreach (var keyValue in names)
		{
			if (source.TryGetValue(keyValue.Key, out var propertyValue) == false) continue;
			source[keyValue.Value] = propertyValue;
			source.Remove(keyValue.Key);
		}

		return sort ? source.ToImmutableSortedDictionary() : source;
	}

}
