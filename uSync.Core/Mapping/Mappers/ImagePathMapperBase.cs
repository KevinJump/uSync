using Jumoo.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text.RegularExpressions;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping;

public abstract class ImagePathMapperBase : SyncValueMapperBase
{
    private readonly IConfiguration _configuration;
    protected readonly ILogger<ImagePathMapperBase> _logger;

    private const string _genericMediaPath = "/media";
    private readonly string _siteRoot;
    private string? _mediaFolder;

    public ImagePathMapperBase(
        IEntityService entityService,
        ILogger<ImagePathMapperBase> logger,
        IConfiguration configuration,
        IOptionsMonitor<GlobalSettings> globalOptions
    ) : base(entityService)
    {
        _configuration = configuration;
        _logger = logger;

        // todo: site root might need us to include extra NuGet.
        _siteRoot = "";

        _mediaFolder = GetMediaFolderSetting(globalOptions.CurrentValue.UmbracoMediaPath.TrimStart('~'));
        globalOptions.OnChange(x => _mediaFolder = GetMediaFolderSetting(x.UmbracoMediaPath.TrimStart('~')));

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Media Folders: [{media}]", _mediaFolder ?? "(Blank)");

    }

    protected string StripSitePath(string filePath)
    {
        var path = filePath;
        if (_siteRoot.Length > 0 && !string.IsNullOrWhiteSpace(filePath) && filePath.InvariantStartsWith(_siteRoot))
            path = filePath.Substring(_siteRoot.Length);

        return ReplacePath(path, _mediaFolder, _genericMediaPath);
    }

    protected string PrePendSitePath(string filePath)
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
    private static string ReplacePath(string filePath, string? currentPath, string? targetPath)
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

    /// <summary>
    ///  Get the actual media file as a dependency. 
    /// </summary>
    public override Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        return uSyncTaskHelper.FromResultOf<IEnumerable<uSyncDependency>>(() =>
        {

            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return [];

            var stringPath = GetImagePath(stringValue).TrimStart('/').ToLower();

            if (!string.IsNullOrWhiteSpace(stringPath))
            {
                return [new uSyncDependency()
                {
                    Name = $"File: {Path.GetFileName(stringPath)}",
                    Udi = Udi.Create(Constants.UdiEntityType.MediaFile, stringPath),
                    Flags = flags,
                    Order = DependencyOrders.OrderFromEntityType(Constants.UdiEntityType.MediaFile),
                    Level = 0
                }];
            }

            return [];
        });
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
