using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

using uSync8.BackOffice.Configuration;
using uSync8.ContentEdition.Extensions;
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
        private readonly IProfilingLogger logger;

        public ImagePathMapper(IEntityService entityService,
            IProfilingLogger logger, uSyncConfig config) : base(entityService)
        {
            this.logger = logger;

            siteRoot = SystemDirectories.Root;
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
                        json["src"] = MediaFolderExtensions.StripSitePath(source, mediaFolder);
                        return JsonConvert.SerializeObject(json);
                    }
                }
            }
            else
            {
                return MediaFolderExtensions.StripSitePath(stringValue, mediaFolder);
            }

            return stringValue;
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
            => config.GetMediaFolderRoot();


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
                        json["src"] = MediaFolderExtensions.PrePendSitePath(source, mediaFolder);
                        return JsonConvert.SerializeObject(json);
                    }
                }
            }
            else
            {
                return MediaFolderExtensions.PrePendSitePath(stringValue, mediaFolder);
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
            if (stringValue.DetectIsJson())
            {
                // json, 
                var json = JsonConvert.DeserializeObject<JObject>(stringValue);
                if (json != null)
                {
                    var source = json.Value<string>("src");
                    if (!string.IsNullOrWhiteSpace(source)) return MediaFolderExtensions.StripSitePath(source, mediaFolder);
                }
            }
            else
            {
                return MediaFolderExtensions.StripSitePath(stringValue, mediaFolder);
            }

            return string.Empty;
        }
    }
}
