using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

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
public class ImagePathMapper : SyncValueMapperBase, ISyncMapper
{
    private const string _genericMediaPath = "/media";

    private readonly string _siteRoot;
    private string _mediaFolder;
    private readonly ILogger<ImagePathMapper> _logger;

    private readonly IConfiguration _configuration;

    public ImagePathMapper(
        IConfiguration configuration,
        IOptionsMonitor<GlobalSettings> _globalOptions,
        IEntityService entityService,
        ILogger<ImagePathMapper> logger) : base(entityService)
    {
        _logger = logger;
        _configuration = configuration;

        // todo: site root might need us to include extra NuGet.
        _siteRoot = "";

        _mediaFolder = GetMediaFolderSetting(_globalOptions.CurrentValue.UmbracoMediaPath.TrimStart('~'));
        _globalOptions.OnChange(x => _mediaFolder = GetMediaFolderSetting(x.UmbracoMediaPath.TrimStart('~')));

        if (!string.IsNullOrWhiteSpace(_mediaFolder))
        {
            logger.LogDebug("Media Folders: [{media}]", _mediaFolder);
        }
    }

    public override string Name => "ImageCropper Mapper";

    public override string[] Editors => [
        Constants.PropertyEditors.Aliases.ImageCropper,
        Constants.PropertyEditors.Aliases.UploadField
    ];

    public override string? GetExportValue(object value, string editorAlias)
    {
        var stringValue = value?.ToString();
        if (string.IsNullOrWhiteSpace(stringValue)) return stringValue;


        if (stringValue.TryParseToJsonObject(out var json) is false || json is null)
            return StripSitePath(stringValue);


        if (json.TryGetPropertyValue("src", out var source) is true && source is not null)
        {
            var sourceString = source.GetValue<string>();

            if (string.IsNullOrWhiteSpace (sourceString) is true)
            {
                json["src"] = StripSitePath(sourceString);
            }
        }

        return json.SerializeJsonNode();

    }

    private string StripSitePath(string filePath)
    {
        var path = filePath;
        if (_siteRoot.Length > 0 && !string.IsNullOrWhiteSpace(filePath) && filePath.InvariantStartsWith(_siteRoot))
            path = filePath.Substring(_siteRoot.Length);

        return ReplacePath(path, _mediaFolder, _genericMediaPath);
    }

    private string PrePendSitePath(string filePath)
    {
        var path = filePath;
        if (_siteRoot.Length > 0 && !string.IsNullOrEmpty(filePath))
            path = $"{_siteRoot}{filePath}";

        return ReplacePath(path, _genericMediaPath, _mediaFolder);
    }


    /// <summary>
    ///  makes a specific media path generic. 
    /// </summary>
    /// <remarks>
    ///  sometimes paths may be defined by umbraco settings, (especially blob settings)
    ///  that mean they are not stored as /media 
    ///  
    ///  for the sake of generic importing we want the folder stored to be /media. 
    ///  so we re-write the setting on import and export 
    ///  
    ///  assumes you have a app setting in the web.config 
    ///  
    ///     <add key="uSync.mediaFolder">/someFolder</add>
    ///
    /// </remarks>
    /// <returns></returns>
    private static string ReplacePath(string filePath, string currentPath, string targetPath)
    {
        if (!string.IsNullOrWhiteSpace(targetPath)
            && !string.IsNullOrWhiteSpace(currentPath)
            && !currentPath.Equals(targetPath))
        {
            return Regex.Replace(filePath, $"^{currentPath}", targetPath, RegexOptions.IgnoreCase);
        }

        return filePath;
    }

    /// <summary>
    ///  Get the media rewrite folder 
    /// </summary>
    /// <remarks>
    ///     looks in appSettings for uSync:mediaFolder 
    ///     
    ///  <add key="uSync.mediaFolder" value="/something" />
    /// 
    ///  or in uSync8.config for media setting 
    ///  
    ///  <backoffice>
    ///     <media>
    ///         <folder>/someFolder</folder>
    ///     </media>
    ///  </backoffice>
    /// </remarks>
    private string GetMediaFolderSetting(string umbracoMediaPath)
    {
        var folder = this._configuration.GetValue<string>("uSync:MediaFolder", string.Empty);
        if (!string.IsNullOrEmpty(folder)) return folder;

        return umbracoMediaPath;
    }


    public override string? GetImportValue(string value, string editorAlias)
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
    }

    /// <summary>
    ///  Get the actual media file as a dependency. 
    /// </summary>
    public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
    {
        var stringValue = value?.ToString();
        if (string.IsNullOrWhiteSpace(stringValue))
            return [];

        var stringPath = GetImagePath(stringValue).TrimStart('/').ToLower();

        if (!string.IsNullOrWhiteSpace(stringPath))
        {
            return new uSyncDependency()
            {
                Name = $"File: {Path.GetFileName(stringPath)}",
                Udi = Udi.Create(Constants.UdiEntityType.MediaFile, stringPath),
                Flags = flags,
                Order = DependencyOrders.OrderFromEntityType(Constants.UdiEntityType.MediaFile),
                Level = 0
            }.AsEnumerableOfOne();
        }

        return [];
    }

    private string GetImagePath(string stringValue)
    {
        if (stringValue.TryParseToJsonObject(out var json) is false || json is null)
            return StripSitePath(stringValue);


        if (json.TryGetPropertyValue("src", out var srcNode) is true)
        {
            var source = srcNode?.GetValue<string>() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(source) is false) return source;
        }

        return string.Empty;
    }
}
