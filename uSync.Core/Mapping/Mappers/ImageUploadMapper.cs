using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Extensions;
using uSync.Core.Serialization;

namespace uSync.Core.Mapping;

/// <summary>
///  image uploads don't store any of the json, stuff, so they are similar to image croppers,
///  but a bit simpler. 
/// </summary>
public class ImageUploadMapper : ImagePathMapperBase, ISyncMapper
{
    public ImageUploadMapper(
        IEntityService entityService,
        ILogger<ImagePathMapperBase> logger,
        IConfiguration configuration,
        IOptionsMonitor<GlobalSettings> globalOptions) : base(entityService, logger, configuration, globalOptions)
    { }

    public override string Name => "Image Upload Mapper";
    public override string[] Editors => [Constants.PropertyEditors.Aliases.UploadField];
    public override Task<string?> GetExportValueAsync(object value, string editorAlias)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Getting export value for ImageUpload with value {Value}", value);

            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue)) return stringValue;
            return StripSitePath(stringValue);
        });
    }

    public override Task<string?> GetImportValueAsync(string value, string editorAlias, SyncSerializerOptions options)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue)) return stringValue;
            return PrePendSitePath(stringValue);
        });
    }
}
