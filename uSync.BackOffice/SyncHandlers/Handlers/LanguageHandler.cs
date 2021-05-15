using Microsoft.Extensions.Logging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    [SyncHandler("languageHandler", "Languages", "Languages", uSyncConstants.Priorites.Languages,
        Icon = "icon-globe", EntityType = UdiEntityType.Language, IsTwoPass = true)]
    public class LanguageHandler : SyncHandlerBase<ILanguage, ILocalizationService>, ISyncHandler,
        INotificationHandler<SavingNotification<ILanguage>>,
        INotificationHandler<SavedNotification<ILanguage>>,
        INotificationHandler<DeletedNotification<ILanguage>>
    {
        private readonly ILocalizationService localizationService;

        public LanguageHandler(
            ILogger<LanguageHandler> logger,
            IEntityService entityService,
            ILocalizationService localizationService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
        {
            this.localizationService = localizationService;
        }

        // language guids are not consistant (at least in alpha)
        // so we don't save by Guid we save by ISO name everytime.           
        protected override string GetPath(string folder, ILanguage item, bool GuidNames, bool isFlat)
        {
            return Path.Combine(folder, $"{this.GetItemPath(item, GuidNames, isFlat)}.config");
        }

        protected override string GetItemPath(ILanguage item, bool useGuid, bool isFlat)
            => item.IsoCode.ToSafeFileName(shortStringHelper);

        protected override IEnumerable<IEntity> GetChildItems(int parent)
        {
            if (parent == -1)
                return localizationService.GetAllLanguages();

            return Enumerable.Empty<IEntity>();
        }

        protected override string GetItemName(ILanguage item) => item.IsoCode;

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
                            logger.LogDebug("Found Matching Lang File, cleaning");
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
                        logger.LogDebug("Language in file is not on the site, cleaning");
                        var attempt = serializer.SerializeEmpty(item, SyncActionType.Delete, node.GetAlias());
                        if (attempt.Success)
                        {
                            syncFileService.SaveXElement(attempt.Item, file);
                        }
                    }
                }
            }
        }

        private static ConcurrentDictionary<string, string> newLanguages = new ConcurrentDictionary<string, string>();

        public void Handle(SavingNotification<ILanguage> notification)
        {
            if (_mutexService.IsPaused) return;

            foreach (var item in notification.SavedEntities)
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

        public override void Handle(SavedNotification<ILanguage> notification)
        {
            if (_mutexService.IsPaused) return;

            foreach (var item in notification.SavedEntities)
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
    }
}
