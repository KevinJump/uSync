using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;
using Umbraco.Core.Services;

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

        public ImagePathMapper(IEntityService entityService) : base(entityService)
        {
            siteRoot = SystemDirectories.Root;
        }

        public override string Name => "ImageCropper Mapper";

        public override string[] Editors => new string[]
        {
            "Umbraco.ImageCropper",
            "Umbraco.UploadField"
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
            if (siteRoot.Length > 0 && !string.IsNullOrWhiteSpace(filepath) && filepath.InvariantStartsWith(siteRoot))
                return filepath.Substring(siteRoot.Length);

            return filepath;
        }

        private string PrePendSitePath(string filepath)
        {
            if (siteRoot.Length > 0 && !string.IsNullOrEmpty(filepath))
                return $"{siteRoot}{filepath}";

            return filepath;
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
    }
}
