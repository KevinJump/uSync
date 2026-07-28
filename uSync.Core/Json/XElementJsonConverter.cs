using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace uSync.Core.Json;

/// <summary>
///  Writes an <see cref="XElement"/> out as a string value.
/// </summary>
/// <remarks>
///  Kept for anything that registers it directly - the copy in Jumoo.Json is the one uSync's
///  own serialization uses, via <c>JsonTextOptions</c>.
/// </remarks>
[Obsolete("Use Jumoo.Json.Converters.JsonXElementConverter - will be removed in v20")]
public class JsonXElementConverter : JsonConverter<XElement>
{
    public override XElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return XElement.Parse(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, XElement value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

