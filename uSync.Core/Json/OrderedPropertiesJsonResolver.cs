using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace uSync.Core.Json;

/// <summary>
///  ordering of the json properties. 
/// </summary>
/// <remarks>
/// <para>
///  we order json properties because it makes for less changes in the physical files
/// </para>
/// <para>
///  less changes in the files = faster comparison checks.
/// </para>
/// </remarks>
internal class OrderedPropertiesJsonResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo typeInfo = base.GetTypeInfo(type, options);

        switch(typeInfo.Kind)
        {
            case JsonTypeInfoKind.Object: 
                return SortObject(typeInfo);
            default:
                return typeInfo;
        }
    }

    private JsonTypeInfo SortObject(JsonTypeInfo typeInfo)
    {
		var order = 0;
		foreach (var property in typeInfo.Properties.OrderBy(x => x.Name))
		{
			property.Order = order++;
		}

		return typeInfo;
	}
}
