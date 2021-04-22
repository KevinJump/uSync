using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("domainHandler", "Domains", "Domains", uSyncBackOfficeConstants.Priorites.DomainSettings
        , Icon = "icon-home usync-addon-icon", EntityType = "domain")]
    public class DomainHandler : SyncHandlerBase<IDomain, IDomainService>, ISyncHandler, ISyncExtendedHandler, ISyncItemHandler
    {
        public override string Group => uSyncBackOfficeConstants.Groups.Content;

        private readonly IDomainService domainService;
        private readonly IShortStringHelper shortStringHelper;

        public DomainHandler(
            IShortStringHelper shortStringHelper,
            ILogger<DomainHandler> logger,
            uSyncConfigService configService,
            AppCaches appCaches,
            ISyncSerializer<IDomain> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService,
            IEntityService entityService,
            IDomainService domainService)
            : base (logger, configService, appCaches, serializer,syncItemFactory, syncFileService, entityService)
        {
            this.shortStringHelper = shortStringHelper;
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
