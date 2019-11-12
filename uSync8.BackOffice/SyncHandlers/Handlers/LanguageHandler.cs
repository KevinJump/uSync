using System;
using System.Collections.Concurrent;
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
using uSync8.Core;
using uSync8.Core.Dependency;
using uSync8.Core.Extensions;
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
        // so we don't save by Guid we save by ISO name everytime.           
        protected override string GetPath(string folder, ILanguage item, bool GuidNames, bool isFlat)
        {
            return $"{folder}/{this.GetItemPath(item, GuidNames, isFlat)}.config";
        }

        protected override string GetItemPath(ILanguage item, bool useGuid, bool isFlat)
            => item.IsoCode.ToSafeFileName();

        protected override void InitializeEvents(HandlerSettings settings)
        {
            // LocalizationService.SavedLanguage += EventSavedItem;
            LocalizationService.DeletedLanguage += EventDeletedItem;

            LocalizationService.SavedLanguage += LocalizationService_SavedLanguage;
            LocalizationService.SavingLanguage += LocalizationService_SavingLanguage;
                
        }

        private static ConcurrentDictionary<string, string> newLanguages = new ConcurrentDictionary<string, string>();


        private void LocalizationService_SavingLanguage(ILocalizationService sender, Umbraco.Core.Events.SaveEventArgs<ILanguage> e)
        {
            foreach(var item in e.SavedEntities)
            {
                // 
                if (item.Id == 0)
                {
                    newLanguages[item.IsoCode] = item.CultureName;
                    // is new, we want to set this as a flag, so we don't do the full content save.n
                    // newLanguages.Add(item.IsoCode);
                }
            }
        }

        private void LocalizationService_SavedLanguage(ILocalizationService sender, Umbraco.Core.Events.SaveEventArgs<ILanguage> e)
        {
            if (uSync8BackOffice.eventsPaused) return;

            foreach (var item in e.SavedEntities)
            {
                bool newItem = false;
                if (newLanguages.Count > 0 && newLanguages.ContainsKey(item.IsoCode))
                {
                    newItem = true;
                    newLanguages.TryRemove(item.IsoCode, out string name);
                }

                if (item.WasPropertyDirty("IsDefault"))
                {
                    // changeing, this change doesn't trigger a save of the other languages.
                    // so we need to save all language files. 
                    this.ExportAll(Path.Combine(rootFolder, DefaultFolder), DefaultConfig, null);
                }


                var attempts = Export(item, Path.Combine(rootFolder, this.DefaultFolder), DefaultConfig);
                foreach(var attempt in attempts.Where(x => x.Success))
                {
                    this.CleanUp(item, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
                }

                if (!newItem && item.WasPropertyDirty(nameof(ILanguage.IsoCode)))
                {
                    // the language code changed, this can mean we need to do a full content media? save. 
                    uSyncTriggers.TriggerExport(rootFolder, new List<string>() { UdiEntityType.Document }, null);
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

        protected override void CleanUp(ILanguage item, string newFile, string folder)
        {
            base.CleanUp(item, newFile, folder);

            // for languages we also clean up by id. 
            // this happens when the language changes .
            var physicalFile = syncFileService.GetAbsPath(newFile);

            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                if (!file.InvariantEquals(physicalFile))
                {
                    var node = syncFileService.LoadXElement(file);
                    if (node.Element("Id").ValueOrDefault(0) == item.Id)
                    {
                        logger.Info<LanguageHandler>("Found Matching Lang File, cleaning");
                        var attempt = serializer.SerializeEmpty(item, SyncActionType.Rename, node.GetAlias());
                        if (attempt.Success)
                        {
                            syncFileService.SaveXElement(attempt.Item, file);
                        }
                    }
                }
            }

        }
    }
}
