using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using Umbraco.Core.Strings;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;
using uSync.Core.Dependency;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.ContentEdition.Handlers
{
    [SyncHandler("domainHandler", "Domains", "Domains", uSyncBackOfficeConstants.Priorites.DomainSettings
        , Icon = "icon-home usync-addon-icon", EntityType = "domain")]
    public class DomainHandler : SyncHandlerBase<IDomain, IDomainService>, ISyncHandler, ISyncExtendedHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IDomainService domainService;

        public DomainHandler(
            IShortStringHelper shortStringHelper,
            IDomainService domainService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<IDomain> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(shortStringHelper, entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
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
            => $"{item.DomainName.ToSafeFileName(shortStringHelper)}_{item.LanguageIsoCode.ToSafeFileName(shortStringHelper)}";

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
