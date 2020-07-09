using System.Collections.Generic;
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

using uSync8.BackOffice.Configuration;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
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

        public ImagePathMapper(IEntityService entityService, 
            IProfilingLogger logger, uSyncConfig config) : base(entityService)
        {
            siteRoot = SystemDirectories.Root;
            mediaFolder = GetMediaFolderSetting(config);

            logger.Debug<ImagePathMapper>("Media Folders: [{media}]", mediaFolder);
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
