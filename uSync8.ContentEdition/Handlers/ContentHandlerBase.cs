using System.IO;
using System.Xml.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.ContentEdition.Serializers;
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Extensions;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Handlers
{
    /// <summary>
    ///  base for all content based handlers
    /// </summary>
    /// <remarks>
    ///  Content based handlers can have the same name in diffrent 
    ///  places around the tree, so we have to check for file name
    ///  clashes. 
    /// </remarks>
    public abstract class ContentHandlerBase<TObject, TService> : SyncHandlerTreeBase<TObject, TService>
        where TObject : IContentBase
        where TService : IService
    {
        protected ContentHandlerBase(
            IEntityService entityService, 
            IProfilingLogger logger, 
            ISyncSerializer<TObject> serializer, 
            ISyncTracker<TObject> tracker, 
            SyncFileService syncFileService) 
            : base(entityService, logger, serializer, tracker, syncFileService)
        { }

        protected ContentHandlerBase(
            IEntityService entityService, 
            IProfilingLogger logger, 
            ISyncSerializer<TObject> serializer, 
            ISyncTracker<TObject> tracker, 
            ISyncDependencyChecker<TObject> checker, 
            SyncFileService syncFileService) 
            : base(entityService, logger, serializer, tracker, checker, syncFileService)
        { }



        protected override string CheckAndFixFileClash(string path, TObject item)
        {
            if (syncFileService.FileExists(path))
            {
                var itemKey = item.Key;
                var node = syncFileService.LoadXElement(path);

                if (node == null) return path;
                if (item.Key == node.GetKey()) return path;
                if (GetXmlMatchString(node) == GetItemMatchString(item)) return path;

                // get here we have a clash, we should append something
                var append = item.Key.ToShortKeyString(8); // (this is the shortened guid like media folders do)
                return Path.Combine(Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path) + "_" + append + Path.GetExtension(path));
            }

            return path;
        }

        protected virtual string GetItemMatchString(TObject item)
        { 
            var level = item.Level;
            if (item.Trashed && serializer is ISyncContentSerializer<TObject> contentSerializer)
            {
                level = contentSerializer.GetLevel(item);
            }
            return $"{item.Name}_{level}".ToLower();
        }

        protected virtual string GetXmlMatchString(XElement node)
            => $"{node.GetAlias()}_{node.GetLevel()}".ToLower();


    }
}
