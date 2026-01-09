using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace uSync.Core.DataTypes;

internal abstract class PickerConfigurationSerializerBase<TContentType> : ConfigurationSerializerBase
    where TContentType : IContentTypeBase
{
    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("filter", out var filterValue) is true)
            configuration["filter"] = ConvertAliasesToGuidValues(filterValue) ?? filterValue;

        if (configuration.TryGetValue("startNodeId", out var startNodeValue) is true)
            configuration["startNodeId"] = ConvertUdiToGuid(startNodeValue) ?? startNodeValue;

        return base.GetConfigurationImport(configuration);
    }

    protected abstract TContentType? GetByAlias(string alias);
    protected string? ConvertAliasesToGuidValues(object? filterValue)
    {
        var item = filterValue?.ToString();
        if (string.IsNullOrWhiteSpace(item)) return item;

        List<string> result = [];

        foreach (var value in item.ToDelimitedList())
        {
            // if this item is already a guid, we return it.
            if (Guid.TryParse(value, out _) is true)
            {
                result.Add(value);
                continue;
            }

            // Lookup the content type by alias - return the guid.
            var mediaType = GetByAlias(value);
            if (mediaType != null)
            {
                result.Add(mediaType.Key.ToString());
                continue;
            }

            result.Add(value);
        }

        return string.Join(',', result);
    }


    /// <summary>
    ///  converts a string from UDI to Guid (if they are set)
    /// </summary>
    protected static string? ConvertUdiToGuid(object? idValue)
    {
        var id = idValue?.ToString();

        if (string.IsNullOrWhiteSpace(id)) return id;
        if (Guid.TryParse(id, out _) is true) return id;

        if (UdiParser.TryParse(id, out GuidUdi? guidUdi) is false || guidUdi is null)
            return id;

        return guidUdi.Guid.ToString();
    }
}
