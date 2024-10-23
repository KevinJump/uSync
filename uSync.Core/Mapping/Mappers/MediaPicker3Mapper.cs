using Microsoft.Extensions.Logging;

using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

using static Umbraco.Cms.Core.Constants;

namespace uSync.Core.Mapping.Mappers;

public class MediaPicker3Mapper : SyncValueMapperBase, ISyncMapper
{
    private readonly ILogger<MediaPicker3Mapper> _logger;

    public MediaPicker3Mapper(
        IEntityService entityService,
        ILogger<MediaPicker3Mapper> logger) : base(entityService)
    {
        _logger = logger;
    }

    public override string Name => "MediaPicker3 Mapper";

    public override string[] Editors => [Constants.PropertyEditors.Aliases.MediaPicker3];

    public override Task<string?> GetExportValueAsync(object value, string editorAlias)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrEmpty(stringValue) is true) return null;

            if (stringValue.TryParseToJsonArray(out var jsonArray) is false || jsonArray is null)
                return stringValue;

            // re-formatting the json in the picker.
            // 
            // we do this because sometimes (and with the starter kit especially)
            // the json might have extra spaces in it, so compared with a server 
            // where this has been imported vs created it can be different but the same.

            // by reading in the json and then spitting it out again, we remove any
            // rouge spacing - so our compare fires through as if nothing has changed.

            if (jsonArray.TrySerializeJsonNode(out string? result))
                return result;

            return stringValue;
        });
    }

    public override Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        return uSyncTaskHelper.FromResultOf<IEnumerable<uSyncDependency>>(() =>
        {
            // validate string 
            var stringValue = value?.ToString();

            if (stringValue.TryParseToJsonArray(out var images) is false || images == null || images.Count == 0)
            {
                _logger.LogWarning("no json images found for {value}", stringValue);
                return [];
            }

            var dependencies = new List<uSyncDependency>();

            foreach (var image in images.AsListOfJsonObjects())
            {
                if (image == null) continue;

                var key = GetGuidValue(image, "mediaKey");
                if (key == Guid.Empty) continue;

                var udi = GuidUdi.Create(UdiEntityType.Media, key);
                var dependency = CreateDependency(udi as GuidUdi, flags);
                if (dependency is not null) dependencies.Add(dependency);
            }

            return dependencies;
        });
    }

    private static Guid GetGuidValue(JsonObject obj, string key)
    {
        if (obj != null && obj.ContainsKey(key))
        {
            var attempt = obj[key]?.ToString().TryConvertTo<Guid>();
            if (attempt?.Success is true)
                return attempt?.Result ?? Guid.Empty;
        }

        return Guid.Empty;
    }
}
