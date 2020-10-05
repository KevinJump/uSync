using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Cache;
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
        Icon = "icon-globe", EntityType = UdiEntityType.Language, IsTwoPass = true)]
    public class LanguageHandler : SyncHandlerBase<ILanguage, ILocalizationService>, ISyncExtendedHandler
    {
        private readonly ILocalizationService localizationService;

        public LanguageHandler(
            ILocalizationService localizationService,
            IEntityService entityService,
            IProfilingLogger logger,
            AppCaches appCaches,
            ISyncSerializer<ILanguage> serializer,
            ISyncItemFactory syncItemFactory,
            SyncFileService syncFileService)
            : base(entityService, logger, appCaches, serializer, syncItemFactory, syncFileService)
        {
            this.localizationService = localizationService;
        }

        [Obsolete("Use constructors with collections")]
        protected LanguageHandler(
            IEntityService entityService,
            IProfilingLogger logger,
            ILocalizationService localizationService,
            ISyncSerializer<ILanguage> serializer,
            ISyncTracker<ILanguage> tracker,
            AppCaches appCaches,
            ISyncDependencyChecker<ILanguage> checker,
            SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, appCaches, checker, syncFileService)
        {
            this.localizationService = localizationService;
        }

        protected override ILanguage GetFromService(int id)
            => localizationService.GetLanguageById(id);


        // language guids are not consistant (at least in alpha)
        // so we don't save by Guid we save by ISO name everytime.           
        protected override string GetPath(string folder, ILanguage item, bool GuidNames, bool isFlat)
        {
            return Path.Combine(folder, $"{this.GetItemPath(item, GuidNames, isFlat)}.config");
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
            if (uSync8BackOffice.eventsPaused) return;

            foreach (var item in e.SavedEntities)
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

                if (!newItem && item.WasPropertyDirty(nameof(ILanguage.IsoCode)))
                {
                    // The language code changed, this can mean we need to do a full content export. 
                    // + we should export the languages again!
                    uSyncTriggers.TriggerExport(rootFolder, new List<string>() {
                        UdiEntityType.Document, UdiEntityType.Language }, null);
                }

                // we always clean up languages, because of the way they are stored. 
                foreach (var attempt in attempts.Where(x => x.Success))
                {
                    this.CleanUp(item, attempt.FileName, Path.Combine(rootFolder, this.DefaultFolder));
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
            var installedLanguages = localizationService.GetAllLanguages()
                .Select(x => x.IsoCode).ToList();

            var files = syncFileService.GetFiles(folder, "*.config");

            foreach (string file in files)
            {
                var node = syncFileService.LoadXElement(file);
                var IsoCode = node.Element("IsoCode").ValueOrDefault(string.Empty);

                if (!String.IsNullOrWhiteSpace(IsoCode))
                {
                    if (!file.InvariantEquals(physicalFile))
                    {
                        // not the file we just saved, but matching IsoCode, we remove it.
                        if (node.Element("IsoCode").ValueOrDefault(string.Empty) == item.IsoCode)
                        {
                            logger.Debug<LanguageHandler>("Found Matching Lang File, cleaning");
                            var attempt = serializer.SerializeEmpty(item, SyncActionType.Rename, node.GetAlias());
                            if (attempt.Success)
                            {
                                syncFileService.SaveXElement(attempt.Item, file);
                            }
                        }
                    }

                    if (!installedLanguages.InvariantContains(IsoCode))
                    {
                        // language is no longer installed, make the file empty. 
                        logger.Debug<LanguageHandler>("Language in file is not on the site, cleaning");
                        var attempt = serializer.SerializeEmpty(item, SyncActionType.Delete, node.GetAlias());
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
