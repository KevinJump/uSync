using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("languageHandler", "Languages", "Languages", uSyncBackOfficeConstants.Priorites.Languages, Icon = "icon-globe")]
    public class LanguageHandler : SyncHandlerBase<ILanguage, ILocalizationService>, ISyncHandler
    {
        private readonly ILocalizationService localizationService;

        public LanguageHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            ILocalizationService localizationService,
            ISyncSerializer<ILanguage> serializer,
            ISyncTracker<ILanguage> tracker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, syncFileService)
        {
            this.localizationService = localizationService;

            this.itemObjectType = UmbracoObjectTypes.Language;
            this.itemContainerType = UmbracoObjectTypes.Unknown;
        }

        protected override ILanguage GetFromService(int id)
            => localizationService.GetLanguageById(id);

        // language guids are not consistant (at least in alpha)
        protected override string GetItemPath(ILanguage item, bool useGuid, bool isFlat)
            => item.IsoCode.ToSafeFileName();

        protected override void InitializeEvents(HandlerSettings settings)
        {
            LocalizationService.SavedLanguage += EventSavedItem;
            LocalizationService.DeletedLanguage += EventDeletedItem;
        }

        protected override IEnumerable<IEntity> GetExportItems(int parent, UmbracoObjectTypes objectType)
        {
            if (parent == -1)
                return localizationService.GetAllLanguages();

            return Enumerable.Empty<IEntity>();
        }

        protected override ILanguage GetFromService(Guid key)
            => null;

        protected override ILanguage GetFromService(string alias)
            => localizationService.GetLanguageByIsoCode(alias);

        protected override void DeleteViaService(ILanguage item)
            => localizationService.Delete(item);

        protected override string GetItemName(ILanguage item)
            => item.IsoCode;
    }
}
