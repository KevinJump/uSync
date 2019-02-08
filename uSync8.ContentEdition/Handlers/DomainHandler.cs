﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("domainHandler", "Domains", "Domains", uSyncBackOfficeConstants.Priorites.DomainSettings
        , Icon = "icon-home usync-addon-icon")]
    public class DomainHandler : SyncHandlerBase<IDomain, IDomainService>, ISyncHandler
    {
        private readonly IDomainService domainService;

        public DomainHandler(IEntityService entityService,
            IProfilingLogger logger, 
            IDomainService domainService,
            ISyncSerializer<IDomain> serializer,
            ISyncTracker<IDomain> tracker,
            SyncFileService syncFileService) 
            : base(entityService, logger, serializer, tracker, syncFileService)
        {
            this.domainService = domainService;
        }

        public override IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config = null)
        {
            var actions = new List<uSyncAction>();

            var domains = domainService.GetAll(true);
            foreach(var domain in domains)
            {
                if (domain != null)
                    actions.Add(Export(domain, folder, config));
            }

            return actions;
        }

        protected override void DeleteViaService(IDomain item)
            => domainService.Delete(item);

        protected override IDomain GetFromService(int id)
            => domainService.GetById(id);

        protected override IDomain GetFromService(Guid key)
            => null;

        protected override IDomain GetFromService(string alias)
            => domainService.GetByName(alias);

        protected override string GetItemName(IDomain item)
            => item.DomainName;

        protected override string GetItemPath(IDomain item, bool useGuid, bool isFlat)
            => item.DomainName.ToSafeFileName();

        protected override void InitializeEvents(HandlerSettings settings)
        {
            DomainService.Saved += EventSavedItem;
            DomainService.Deleted += EventDeletedItem;
        }
    }
}
