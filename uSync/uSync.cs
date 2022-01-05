using System;

using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Packaging;

namespace uSync
{
    /// <summary>
    ///  we only have this class, so there is a dll in the root
    ///  uSync package.
    ///  
    ///  With a root dll, the package can be stopped from installing
    ///  on .netframework sites.
    /// </summary>
    public static class uSync
    {
        public static string PackageName = "uSync";
        // private static string Welcome = "uSync all the things";
    }

    /// <summary>
    ///  A package migration plan, allows us to put uSync in the list 
    ///  of installed packages. we don't actually need a migration 
    ///  for uSync (doesn't add anything to the db). but by doing 
    ///  this people can see that it is insalled. 
    /// </summary>
    public class uSyncMigrationPlan : PackageMigrationPlan
    {
        public uSyncMigrationPlan() :
            base(uSync.PackageName)
        { }

        protected override void DefinePlan()
        {
            To<SetupuSync>(new Guid("65735030-E8F2-4F34-B28A-2201AF9792BE"));
        }
    }

    public class SetupuSync : PackageMigrationBase
    {
        public SetupuSync(
            IPackagingService packagingService, 
            IMediaService mediaService, 
            MediaFileManager mediaFileManager, 
            MediaUrlGeneratorCollection mediaUrlGenerators, 
            IShortStringHelper shortStringHelper, 
            IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
            IMigrationContext context) 
            : base(packagingService, mediaService, mediaFileManager, mediaUrlGenerators, shortStringHelper, contentTypeBaseServiceProvider, context)
        {
        }

        protected override void Migrate()
        {
            // we don't actually need to do anything, but this means we end up
            // on the list of installed packages. 
        }
    }
}
