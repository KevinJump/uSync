using System.Text.Json;
using System.Text.Json.Serialization;

using Umbraco.Cms.Core.Models.Blocks;

namespace uSync.Core.Json;

public class JsonBlockGridLayoutItemConverter : JsonConverter<BlockGridLayoutItem>
{
    public override BlockGridLayoutItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Invalid JSON expecting start object");

        var item = new BlockGridLayoutItem();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return item;
            
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Invalid JSON expecting property name");

            var propertyName = reader.GetString();
            reader.Read();
            switch (propertyName)
            {
                case "areas":
                    var areas = JsonSerializer.Deserialize<List<BlockGridLayoutAreaItem>>(ref reader, options);
                    if (areas != null)
                        item.Areas = [.. areas];
                    break;
                
                case "columnSpan":
                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var colSpan))
                        ((BlockGridLayoutItem)item).ColumnSpan = colSpan;
                    break;
                
                case "rowSpan":
                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var rowSpan))
                        ((BlockGridLayoutItem)item).RowSpan = rowSpan;
                    break;
                
                case "contentKey":
                    var contentKey = reader.GetString();
                    if (contentKey != null && Guid.TryParse(contentKey, out var contentGuid))
                        item.ContentKey = contentGuid;
                    break;
                
                case "settingsKey":
                    var settingsKey = reader.GetString();
                    if (settingsKey != null && Guid.TryParse(settingsKey, out var settingsGuid))
                        item.SettingsKey = settingsGuid;
                    break;
                
                default:
                    // we don't care about the obsolete properties here...
                    break;
            }
        }

        throw new JsonException("Unexpected end of JSON block");
    }

    public override void Write(Utf8JsonWriter writer, BlockGridLayoutItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("areas");
        JsonSerializer.Serialize(writer, value.Areas, options);

        if (value.ColumnSpan != null)
        {
            writer.WritePropertyName("columnSpan");
            writer.WriteNumberValue(value.ColumnSpan.Value);
        }

        if (value.RowSpan != null)
        {
            writer.WritePropertyName("rowSpan");
            writer.WriteNumberValue(value.RowSpan.Value);
        }

        writer.WriteString("contentKey", value.ContentKey.ToString());
        writer.WriteNull("contentUdi");
        if (value.SettingsKey.HasValue && value.SettingsKey != Guid.Empty)
            writer.WriteString("settingsKey", value.SettingsKey.ToString());
        else
            writer.WriteNull("settingsKey");
        writer.WriteEndObject();
    }
}
