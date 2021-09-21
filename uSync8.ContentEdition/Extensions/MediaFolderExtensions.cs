using Serilog.Core;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uSync8.BackOffice.Configuration;
using uSync8.ContentEdition.Mapping.Mappers;

namespace uSync8.ContentEdition.Extensions
{
    public static class MediaFolderExtensions
    {
        /// <summary>
        ///  calculate the root of media storage. 
        /// </summary>
        /// <remarks>
        ///  depending on azure config settings, this can be multiple folders, where /media would be
        ///  or it can be a URL part.
        ///  
        ///  to make uSync generic we want this to go back to /media, so we get the root folder
        ///  and in certain places in the code, we strip it out. only to later put it back in.
        /// </remarks>

        public static string GetMediaFolderRoot(this uSyncConfig config)
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
                    return $"/{containerName}";
                }
            }

            // more azure guessing - if the virtual path provider is disabled, 
            // then the URLs are stored in the media store, and we need to 
            // strip them from the names when we save things. 
            var disableVirtualProvider = ConfigurationManager.AppSettings["AzureBlobFileSystem.DisableVirtualPathProvider"];
            if (disableVirtualProvider != null && bool.TryParse(disableVirtualProvider, out bool virtualProviderDisabled) && virtualProviderDisabled)
            {
                // the virutal provider is disabled so media will be stored by the url

                var mediaRoot = ConfigurationManager.AppSettings["AzureBlobFileSystem.RootUrl:media"];
                var containerName = ConfigurationManager.AppSettings["AzureBlobFileSystem.ContainerName:media"];

                if (!string.IsNullOrWhiteSpace(containerName) && !string.IsNullOrWhiteSpace(mediaRoot))
                {
                    return $"{mediaRoot}{containerName}";
                }
            }

            return config.GetExtensionSetting("media", "folder", string.Empty);

        }
    }
}
