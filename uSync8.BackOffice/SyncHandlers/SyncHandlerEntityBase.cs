using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using uSync8.Core;
using uSync8.BackOffice.Services;
using Umbraco.Core.Models;
using uSync8.Core.Serialization;
using System.Xml.Linq;

namespace uSync8.BackOffice.SyncHandlers
{
    public abstract class SyncHandlerEntityBase<TObject, TService> : SyncHandlerBase<TObject, TService>, IDiscoverable
        where TObject : IUmbracoEntity
        where TService : IService
    {

        protected SyncHandlerEntityBase(
            IEntityService entityService,
            IProfilingLogger logger,
            ISyncSerializer<TObject> serializer,
            SyncFileService syncFileService,
            uSyncBackOfficeSettings settings)
            : base(entityService, logger, serializer, syncFileService, settings)
        {
        }


        virtual protected string GetItemFileName(IUmbracoEntity item)
        {
            if (item != null)
            {
                if (globalSettings.UseFlatStructure)
                    return item.Key.ToString();

                return item.Name.ToSafeFileName();
            }

            return Guid.NewGuid().ToString();
        }

        override protected string GetItemPath(TObject item)
        {
            if (globalSettings.UseFlatStructure)
                return GetItemFileName((IUmbracoEntity)item);

            return GetEntityPath((IUmbracoEntity)item);
        }

        protected string GetEntityPath(IUmbracoEntity item)
        {
            var path = string.Empty;
            if (item != null)
            {
                if (item.ParentId > 0)
                {
                    var parent = entityService.Get(item.ParentId);
                    if (parent != null)
                    {
                        path = GetEntityPath(parent);
                    }
                }

                path = Path.Combine(path, GetItemFileName(item));
            }

            return path;
        }
    }
}
