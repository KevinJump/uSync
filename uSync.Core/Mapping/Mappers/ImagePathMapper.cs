using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping
{
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
        private const string s_genericMediaPath = "/media";

        private readonly string siteRoot;
        private string _mediaFolder;
        private readonly ILogger<ImagePathMapper> logger;

        private readonly IConfiguration configuration;

        public ImagePathMapper(
            IConfiguration configuration,
            IOptionsMonitor<GlobalSettings> _globalOptions,
            IEntityService entityService,
            ILogger<ImagePathMapper> logger) : base(entityService)
        {
            this.logger = logger;
            this.configuration = configuration;

            // todo: site root might need us to include extra nuget.
            siteRoot = "";

            _mediaFolder = GetMediaFolderSetting(_globalOptions.CurrentValue.UmbracoMediaPath.TrimStart('~'));
            _globalOptions.OnChange(x => _mediaFolder = GetMediaFolderSetting(x.UmbracoMediaPath.TrimStart('~')));

            if (!string.IsNullOrWhiteSpace(_mediaFolder))
            {
                logger.LogDebug("Media Folders: [{media}]", _mediaFolder);
            }
        }

        public override string Name => "ImageCropper Mapper";

        public override string[] Editors => new string[]
        {
            Constants.PropertyEditors.Aliases.ImageCropper,
            Constants.PropertyEditors.Aliases.UploadField
        };

        public override string GetExportValue(object value, string editorAlias)
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue)) return stringValue;

            if (stringValue.TryParseValidJsonString<JObject>(out JObject json) is false)
                return StripSitePath(stringValue);

            // json, 
            if (json != null)
            {
                var source = json.Value<string>("src");
                if (!string.IsNullOrWhiteSpace(source))
                {
                    // strip any virtual directory stuff from it.
                    json["src"] = StripSitePath(source);
                    return JsonConvert.SerializeObject(json);
                }
            }

            // we always reserialize if we can, because you can get inconsitancies, 
            // and spaces in the json (especially from the starterkit)
            // this just ensures it looks the same across sites (where possible).
            return JsonConvert.SerializeObject(json, Formatting.Indented);
        }

        private string StripSitePath(string filepath)
        {
            var path = filepath;
            if (siteRoot.Length > 0 && !string.IsNullOrWhiteSpace(filepath) && filepath.InvariantStartsWith(siteRoot))
                path = filepath.Substring(siteRoot.Length);

            return ReplacePath(path, _mediaFolder, s_genericMediaPath);
        }

        private string PrePendSitePath(string filepath)
        {
            var path = filepath;
            if (siteRoot.Length > 0 && !string.IsNullOrEmpty(filepath))
                path = $"{siteRoot}{filepath}";

            return ReplacePath(path, s_genericMediaPath, _mediaFolder);
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
        ///     <add key="uSync.mediaFolder">/somefolder</add>
        ///
        /// </remarks>
        /// <returns></returns>
        private string ReplacePath(string filepath, string currentPath, string targetPath)
        {
            if (!string.IsNullOrWhiteSpace(targetPath)
                && !string.IsNullOrWhiteSpace(currentPath)
                && !currentPath.Equals(targetPath))
            {
                return Regex.Replace(filepath, $"^{currentPath}", targetPath, RegexOptions.IgnoreCase);
            }

            return filepath;
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
        ///         <folder>/somefolder</folder>
        ///     </media>
        ///  </backoffice>
        /// </remarks>
        private string GetMediaFolderSetting(string umbracoMediaPath)
        {
            var folder = this.configuration.GetValue<string>("uSync:MediaFolder", string.Empty);
            if (!string.IsNullOrEmpty(folder)) return folder;

            return umbracoMediaPath;
        }


        public override string GetImportValue(string value, string editorAlias)
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue) is true) return stringValue;

            if (stringValue.TryParseValidJsonString(out JObject json) is false)
                return PrePendSitePath(stringValue);

            var source = json.Value<string>("src");
            if (string.IsNullOrWhiteSpace(source) is false)
            {
                // strip any virtual directory stuff from it.
                json["src"] = PrePendSitePath(source);
                return JsonConvert.SerializeObject(json);
            }

            return stringValue;
        }

        /// <summary>
        ///  Get the actual media file as a dependency. 
        /// </summary>
        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return Enumerable.Empty<uSyncDependency>();

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

            return Enumerable.Empty<uSyncDependency>();
        }

        private string GetImagePath(string stringValue)
        {
            if (stringValue.TryParseValidJsonString(out JObject json))
            {
                var source = json.Value<string>("src");
                if (string.IsNullOrWhiteSpace(source) is false) return source;
            }
            else
            {
                return StripSitePath(stringValue);
            }

            return string.Empty;
        }
    }
}
