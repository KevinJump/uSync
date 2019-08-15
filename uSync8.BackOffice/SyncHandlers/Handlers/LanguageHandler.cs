using System;
using System.Collections.Generic;
using System.IO;
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
using uSync8.Core.Dependency;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;
using static Umbraco.Core.Constants;

namespace uSync8.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("languageHandler", "Languages", "Languages", uSyncBackOfficeConstants.Priorites.Languages,
        Icon = "icon-globe", EntityType = UdiEntityType.Language)]
    public class LanguageHandler : SyncHandlerBase<ILanguage, ILocalizationService>, ISyncExtendedHandler
    {
        private readonly ILocalizationService localizationService;

        public LanguageHandler(
            IEntityService entityService, 
            IProfilingLogger logger, 
            ILocalizationService localizationService,
            ISyncSerializer<ILanguage> serializer,
            ISyncTracker<ILanguage> tracker,
            ISyncDependencyChecker<ILanguage> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, checker, syncFileService)
        {
            this.localizationService = localizationService;
        }

        protected override ILanguage GetFromService(int id)
            => localizationService.GetLanguageById(id);

        // language guids are not consistant (at least in alpha)
        protected override string GetItemPath(ILanguage item, bool useGuid, bool isFlat)
            => item.IsoCode.ToSafeFileName();

        protected override void InitializeEvents(HandlerSettings settings)
        {
            // LocalizationService.SavedLanguage += EventSavedItem;
            LocalizationService.DeletedLanguage += EventDeletedItem;

            LocalizationService.SavedLanguage += LocalizationService_SavedLanguage;
        }

        private void LocalizationService_SavedLanguage(ILocalizationService sender, Umbraco.Core.Events.SaveEventArgs<ILanguage> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            foreach (var item in e.SavedEntities)
            {
                if (item.WasPropertyDirty("IsDefault"))
                {
                    // changeing, this change doesn't trigger a save of the other languages.
                    // so we need to save all language files. 
                    this.ExportAll(Path.Combine(rootFolder, DefaultFolder), DefaultConfig, null);
                }
                else
                {
                    var attempt = Export(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
                    if (attempt.Success)
                    {
                        this.CleanUp(item, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                    }
                }
            }
        }

        protected override IEnumerable<IEntity> GetChildItems(int parent)
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
