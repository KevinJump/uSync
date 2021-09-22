using System.Configuration;
using System.Text.RegularExpressions;

using Umbraco.Core;
using Umbraco.Core.IO;

using uSync8.BackOffice.Configuration;

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

            return config.GetExtensionSetting("media", "folder", string.Empty);

        }

      

        public static string StripSitePath(string filepath, string mediaFolder)
        {
            var path = filepath;
            if (SystemDirectories.Root.Length > 0 && !string.IsNullOrWhiteSpace(filepath) && filepath.InvariantStartsWith(SystemDirectories.Root))
                path = filepath.Substring(SystemDirectories.Root.Length);

            return ReplacePath(path, mediaFolder, "/media");
        }


        public static string PrePendSitePath(string filepath, string mediaFolder)
        {
            var path = filepath;
            if (SystemDirectories.Root.Length > 0 && !string.IsNullOrEmpty(filepath))
                path = $"{SystemDirectories.Root}{filepath}";

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
        private static string ReplacePath(string filepath, string currentPath, string targetPath)
        {
            if (!string.IsNullOrWhiteSpace(targetPath)
                && !string.IsNullOrWhiteSpace(currentPath))
            {
                return Regex.Replace(filepath, $"^{currentPath}", targetPath, RegexOptions.IgnoreCase);
            }

            return filepath;
        }

    }
}
