using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Text;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Media;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace uSync.Core.Versions;

/// <inheritdoc/>
public class SyncImageUpdateHelper : ISyncImageUpdateHelper
{
    private readonly IImageUrlGenerator _imageUrlGenerator;
    private readonly ImagingSettings _imagingSettings;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly ILogger<SyncImageUpdateHelper> _logger;
    
    public SyncImageUpdateHelper(
        IImageUrlGenerator imageUrlGenerator,
        IOptions<ImagingSettings> imagingSettings,
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedUrlProvider publishedUrlProvider,
        ILogger<SyncImageUpdateHelper> logger)
    {
        _imageUrlGenerator = imageUrlGenerator;
        _imagingSettings = imagingSettings.Value;
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedUrlProvider = publishedUrlProvider;
        _logger = logger;
    }

    public string GetImageHmacString(string value)
    {
        if (_imagingSettings.HMACSecretKey.Length == 0)
            return string.Empty;

        byte[] signature = Encoding.UTF8.GetBytes(value);
        using (HMACSHA256 HMAC = new(_imagingSettings.HMACSecretKey))
        {
            byte[] signatureBytes = HMAC.ComputeHash(signature);
            return Convert.ToBase64String(signatureBytes);
        }
    }
        

    /// <inheritdoc/>
    public string UpdateImageUrlValues(string html)
    {
        // if this isn't configured, there is no point in doing any work, so we just return the original html.
        if (_imagingSettings.HMACSecretKey.Length == 0)
            return html;

        try
        {

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes("//img[@data-udi]");
            if (nodes is null) return html;

            foreach (var img in nodes)
            {
                var udiString = img.GetAttributeValue("data-udi", string.Empty);
                if (string.IsNullOrEmpty(udiString)) continue;

                if (UdiParser.TryParse(udiString, out GuidUdi? udi) is false || udi is null)
                    continue;

                if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) is false) continue;

                IPublishedContent? media = umbracoContext?.Media?.GetById(udi.Guid);
                if (media is null)
                {
                    _logger.LogWarning("Could not find media item with UDI {Udi} for image URL generation.", udiString);
                    continue;
                }

                var location = media.Url(_publishedUrlProvider);
                var width = img.GetAttributeValue("width", int.MinValue);
                var height = img.GetAttributeValue("height", int.MinValue);

                if (width != int.MinValue && height != int.MinValue)
                {
                    location = _imageUrlGenerator.GetImageUrl(new ImageUrlGenerationOptions(location)
                    {
                        ImageCropMode = ImageCropMode.Max,
                        Width = width,
                        Height = height
                    });
                }

                img.SetAttributeValue("src", location ?? string.Empty);
            }

            return doc.DocumentNode.OuterHtml;
        }
        catch(Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while updating image URLs in HTML content. Returning original HTML.");
            return html;
        }
    }
}
