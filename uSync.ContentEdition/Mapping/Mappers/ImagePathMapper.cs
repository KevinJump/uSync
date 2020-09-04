using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync.BackOffice.Configuration;
using uSync.Core.Dependency;

using static Umbraco.Core.Constants;

namespace uSync.ContentEdition.Mapping.Mappers
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
        private readonly string siteRoot;
        private readonly string mediaFolder;
        private readonly IProfilingLogger logger;

        public ImagePathMapper(IEntityService entityService, 
            SyncValueMapperFactory mapperFactory,
            IProfilingLogger logger, uSyncConfig config) : base(entityService, mapperFactory)
        {
            this.logger = logger;

            // siteRoot = SystemDirectories.Root;
            siteRoot = "";
            mediaFolder = GetMediaFolderSetting(config);

            if (!string.IsNullOrWhiteSpace(mediaFolder))
            {
                logger.Debug<ImagePathMapper>("Media Folders: [{media}]", mediaFolder);
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

            if (stringValue.DetectIsJson())
            {
                // json, 
                var json = JsonConvert.DeserializeObject<JObject>(stringValue);
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
            }
            else
            {
                return StripSitePath(stringValue);
            }

            return stringValue;
        }

        private string StripSitePath(string filepath)
        {
            var path = filepath;
            if (siteRoot.Length > 0 && !string.IsNullOrWhiteSpace(filepath) && filepath.InvariantStartsWith(siteRoot))
                path = filepath.Substring(siteRoot.Length);

            return ReplacePath(path, mediaFolder, "/media");
        }

        private string PrePendSitePath(string filepath)
        {
            var path = filepath;
            if (siteRoot.Length > 0 && !string.IsNullOrEmpty(filepath))
                path = $"{siteRoot}{filepath}";

            return ReplacePath(path, "/media", mediaFolder);
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
                && !string.IsNullOrWhiteSpace(currentPath))
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
        private string GetMediaFolderSetting(uSyncConfig config)
        {
            // look in the web.config 
            var folder = ConfigurationManager.AppSettings["uSync.mediaFolder"];
            if (!string.IsNullOrWhiteSpace(folder))
                return folder;

            // azure guessing - so for most people seeing this issue, 
            // they won't have to do any config, as we will detect it ???
            var useDefault = ConfigurationManager.AppSettings["AzureBlobFileSystem.UseDefaultRoute:media"];
            if (useDefault != null && bool.TryParse(useDefault, out bool usingDefaultRoute) && !usingDefaultRoute)
            {
                // means azure is configured to not use the default root, so the media 
                // will be prepended with /container-name. 
                var containerName = ConfigurationManager.AppSettings["AzureBlobFileSystem.ContainerName:media"];
                if (!string.IsNullOrWhiteSpace(containerName))
                {
                    logger.Debug<ImagePathMapper>("Calculating media folder path from AzureBlobFileSystem settings");
                    return $"/{containerName}";
                }
            }
            

            // look in the uSync8.config 
            return config.GetExtensionSetting("media", "folder", string.Empty);
        }


        public override string GetImportValue(string value, string editorAlias)
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue)) return stringValue;

            if (stringValue.DetectIsJson())
            {
                // json, 
                var json = JsonConvert.DeserializeObject<JObject>(stringValue);
                if (json != null)
                {
                    var source = json.Value<string>("src");
                    if (!string.IsNullOrWhiteSpace(source))
                    {
                        // strip any virtual directory stuff from it.
                        json["src"] = PrePendSitePath(source);
                        return JsonConvert.SerializeObject(json);
                    }
                }
            }
            else
            {
                return PrePendSitePath(stringValue);
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

            var stringPath = GetImagePath(stringValue).TrimStart('/').ToLower() ;

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
            if (stringValue.DetectIsJson())
            {
                // json, 
                var json = JsonConvert.DeserializeObject<JObject>(stringValue);
                if (json != null)
                {
                    var source = json.Value<string>("src");
                    if (!string.IsNullOrWhiteSpace(source)) return source;
                }
            }
            else
            {
                return StripSitePath(stringValue);
            }

            return string.Empty;
        }
    }
}
