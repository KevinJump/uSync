using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.BackOffice.SyncHandlers;
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.ContentEdition.Handlers
{
    [SyncHandler("domainHandler", "Domains", "Domains", uSyncBackOfficeConstants.Priorites.DomainSettings
        , Icon = "icon-home usync-addon-icon", EntityType = "domain")]
    public class DomainHandler : SyncHandlerBase<IDomain, IDomainService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IDomainService domainService;

        public DomainHandler(IEntityService entityService,
            IProfilingLogger logger,
            IDomainService domainService,
            ISyncSerializer<IDomain> serializer,
            ISyncTracker<IDomain> tracker,
            ISyncDependencyChecker<IDomain> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, checker, syncFileService)
        {
            this.domainService = domainService;
        }

        public override IEnumerable<uSyncAction> ExportAll(string folder, HandlerSettings config, SyncUpdateCallback callback)
        {
            var actions = new List<uSyncAction>();

            var domains = domainService.GetAll(true).ToList();
            int count = 0;
            foreach (var domain in domains)
            {
                count++;
                if (domain != null)
                {
                    callback?.Invoke(domain.DomainName, count, domains.Count);
                    actions.AddRange(Export(domain, folder, config));
                }
            }

            callback?.Invoke("done", 1, 1);
            return actions;
        }

        protected override void DeleteViaService(IDomain item)
            => domainService.Delete(item);

        protected override IDomain GetFromService(int id)
            => domainService.GetById(id);

        protected override IDomain GetFromService(Guid key)
            => domainService.GetAll(true).FirstOrDefault(x => x.Key == key);

        protected override Guid GetItemKey(IDomain item)
            => item.Id.ToGuid();

        protected override IDomain GetFromService(string alias)
            => domainService.GetByName(alias);

        protected override string GetItemName(IDomain item)
            => item.DomainName;

        protected override string GetItemPath(IDomain item, bool useGuid, bool isFlat)
            => $"{item.DomainName.ToSafeFileName()}_{item.LanguageIsoCode.ToSafeFileName()}";

        protected override void InitializeEvents(HandlerSettings settings)
        {
            DomainService.Saved += EventSavedItem;
            DomainService.Deleted += EventDeletedItem;
        }
        protected override IEnumerable<IEntity> GetChildItems(int parent)
        {
            if (parent == -1)
                return domainService.GetAll(true)
                    .Where(x => x is IEntity)
                    .Select(x => x as IEntity);

            return base.GetChildItems(parent);

        }
    }
}
