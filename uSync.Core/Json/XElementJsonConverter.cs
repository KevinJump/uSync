using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace uSync.Core.Json;

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
