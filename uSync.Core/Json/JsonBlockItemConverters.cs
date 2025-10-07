using System.Text.Json;
using System.Text.Json.Serialization;

using Umbraco.Cms.Core.Models.Blocks;

namespace uSync.Core.Json;

/// <summary>
/// in v16 the BlockLayoutItem(s) have obsolete properties that are sometimes set and sometimes not
/// this leads to false positive's when looking for changes. 
/// 
///  these two custom converters write those properties out as null, so they never change. causing
///  the serialized json to be consistent. 
/// </summary>

public abstract class JsonBlockItemConverterBase<T> : JsonConverter<T>
    where T : BlockLayoutItemBase, new()
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Invalid JSON expecting start object");

        var item = new T();

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

        throw new JsonException("Unexpected end of JSON");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // we could just not write out the obsolete properties, but they will appear as null it 
        // lots of people's existing exports , so we can write them out as null to keep things consistent.
        writer.WriteStartObject();
        writer.WriteString("contentKey", value.ContentKey.ToString());
        writer.WriteNull("contentUdi");

        if (value.SettingsKey.HasValue && value.SettingsKey != Guid.Empty)
            writer.WriteString("settingsKey", value.SettingsKey.ToString());
        else
            writer.WriteNull("settingsKey");

        writer.WriteNull("settingsUdi");
        writer.WriteEndObject();
    }
}

public class JsonBlockListLayoutItemConverter : JsonBlockItemConverterBase<BlockListLayoutItem> { }

public class JsonBlockGridLayoutItemConverter : JsonBlockItemConverterBase<BlockGridLayoutItem> { }
