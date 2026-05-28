using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Media;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Extensions;
using uSync.Core.Serialization;

namespace uSync.Core.Mapping;

/// <summary>
///  Mapper for file path in an image cropper / upload control
/// </summary>
/// <remarks>
///  this removes / adds any virtual folder properties 
///  to a path (so if you have your umbraco install in virtual folder paths)
///  
/// {"src":"/subfolder/media/2cud1lzo/15656993711_ccd199b83e_k.jpg","crops":null}
/// becomes 
/// {"src":"/media/2cud1lzo/15656993711_ccd199b83e_k.jpg","crops":null}
/// </remarks>
public class ImagePathMapper : ImagePathMapperBase, ISyncMapper
{
    private readonly IImageUrlGenerator _imageUrlGenerator;

    public ImagePathMapper(
        IEntityService entityService,
        ILogger<ImagePathMapper> logger,
        IConfiguration configuration,
        IOptionsMonitor<GlobalSettings> globalOptions,
        IImageUrlGenerator imageUrlGenerator) : base(entityService, logger, configuration, globalOptions)
    {
        _imageUrlGenerator = imageUrlGenerator;
    }

    public override string Name => "ImageCropper Mapper";

    public override string[] Editors => [
        Constants.PropertyEditors.Aliases.ImageCropper
    ];

    public override Task<string?> GetExportValueAsync(object value, string editorAlias)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue)) return stringValue;

            if (stringValue.TryParseToJsonObject(out var json) is false || json is null)
            {
                var extension = Path.GetExtension(stringValue);
                if (string.IsNullOrWhiteSpace(extension)) return stringValue;

                if (_imageUrlGenerator.IsSupportedImageFormat(extension.TrimStart(Constants.CharArrays.Period)) is true) 
                {
                    // its a 'bug' that things imported via starter kits etc, can end up going in without the image cropper json structure.
                    // if we find a straight image path for a supported image format, we convert it to the json structure here.
                    var cropper = new ImageCropperValue() { Src = StripSitePath(stringValue) };
                    return cropper.SerializeJsonString();
                }

                // we can only convert things that support the cropper.
                // So if its not one of them we put them back as we find them.
                return stringValue;
            }
           

            if (json.TryGetPropertyValue("src", out var source) is true && source is not null)
            {
                var sourceString = source.GetValue<string>();

                if (string.IsNullOrWhiteSpace(sourceString) is true)
                {
                    json["src"] = StripSitePath(sourceString);
                }
            }

            return json.SerializeJsonNode();
        });
    }


    public override Task<string?> GetImportValueAsync(string value, string editorAlias, SyncSerializerOptions options)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue) is true) return stringValue;

            if (stringValue.TryParseToJsonObject(out var json) is false || json is null)
                return PrePendSitePath(stringValue);

            if (json.TryGetPropertyValue("src", out var srcNode) is true)
            {
                var source = srcNode?.GetValue<string>() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(source) is false)
                {
                    // strip any virtual directory stuff from it.
                    json["src"] = PrePendSitePath(source);
                }
            }

            return json.SerializeJsonNode(true);
        });
    }
}
