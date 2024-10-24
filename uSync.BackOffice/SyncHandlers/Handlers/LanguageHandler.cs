using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

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
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Extensions;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to mange language settings in uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.LanguageHandler, "Languages", "Languages", uSyncConstants.Priorites.Languages,
    Icon = "icon-globe", EntityType = UdiEntityType.Language, IsTwoPass = true)]
public class LanguageHandler : SyncHandlerBase<ILanguage>, ISyncHandler,
    INotificationAsyncHandler<SavingNotification<ILanguage>>,
    INotificationAsyncHandler<SavedNotification<ILanguage>>,
    INotificationAsyncHandler<DeletedNotification<ILanguage>>,
    INotificationAsyncHandler<DeletingNotification<ILanguage>>
{
    private readonly ILanguageService _languageService;

    /// <inheritdoc/>
    public LanguageHandler(
        ILogger<LanguageHandler> logger,
        IEntityService entityService,
        ILanguageService languageService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        uSyncEventService mutexService,
        uSyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    {
        _languageService = languageService;
    }

    /// <inheritdoc/>
    // language guids are not consistent (at least in alpha)
    // so we don't save by Guid we save by ISO name every time.           
    protected override Task<string> GetPathAsync(string folder, ILanguage item, bool GuidNames, bool isFlat)
        => uSyncTaskHelper.FromResultOf(()
            => Path.Combine(folder, $"{this.GetItemPath(item, GuidNames, isFlat)}.{this.uSyncConfig.Settings.DefaultExtension}"));

    /// <inheritdoc/>
    protected override string GetItemPath(ILanguage item, bool useGuid, bool isFlat)
        => item.IsoCode.ToSafeFileName(shortStringHelper);

	/// <summary>
	///  order the merged items, making sure the default language is first. 
	/// </summary>
	protected override async Task<IReadOnlyList<OrderedNodeInfo>> GetMergedItemsAsync(string[] folders)
		=> (await base.GetMergedItemsAsync(folders))
			.OrderBy(x => x.Node.Element("IsDefault").ValueOrDefault(false) ? 0 : 1)
			.ToList();

	/// <summary>
	///  ensure we import the 'default' language first, so we don't get errors doing it. 
	/// </summary>
	/// <remarks>
	///  prost v13.1 this method isn't used to determine the order for all options.
	/// </remarks>
	protected override IEnumerable<string> GetImportFiles(string folder)
    {
        var files = base.GetImportFiles(folder);

        try
        {
            Dictionary<string, string> ordered = [];
            foreach (var file in files)
            {
                var node = XElement.Load(file);
                var order = (node.Element("IsDefault").ValueOrDefault(false) ? "0" : "1") + Path.GetFileName(file);
                ordered[file] = order;
            }

            return ordered.OrderBy(x => x.Value).Select(x => x.Key).ToList();
        }
        catch
        {
            return files;
        }

    }


    protected override async Task<IEnumerable<IEntity>> GetChildItemsAsync(Guid key)
        => key == Guid.Empty
            ? await _languageService.GetAllAsync()
            : Enumerable.Empty<IEntity>();

    /// <inheritdoc/>
    protected override string GetItemName(ILanguage item) => item.IsoCode;

    /// <inheritdoc/>
    protected override async Task CleanUpAsync(ILanguage item, string newFile, string folder)
    {
        await base.CleanUpAsync(item, newFile, folder);

        // for languages we also clean up by id. 
        // this happens when the language changes .
        var physicalFile = syncFileService.GetAbsPath(newFile);
        var installedLanguages = (await _languageService.GetAllAsync())
            .Select(x => x.IsoCode).ToList();

        var files = syncFileService.GetFiles(folder, $"*.{this.uSyncConfig.Settings.DefaultExtension}");

        foreach (string file in files)
        {
            var node = await syncFileService.LoadXElementAsync(file);
            var IsoCode = node.Element("IsoCode").ValueOrDefault(string.Empty);

            if (!String.IsNullOrWhiteSpace(IsoCode))
            {
                if (!file.InvariantEquals(physicalFile))
                {
                    // not the file we just saved, but matching IsoCode, we remove it.
                    if (node.Element("IsoCode").ValueOrDefault(string.Empty) == item.IsoCode)
                    {
                        logger.LogDebug("Found Matching Lang File, cleaning");
                        var attempt = await serializer.SerializeEmptyAsync(item, SyncActionType.Rename, node.GetAlias());
                        if (attempt.Success && attempt.Item is not null)
                        {
                            await syncFileService.SaveXElementAsync(attempt.Item, file);
                        }
                    }
                }

                if (!installedLanguages.InvariantContains(IsoCode))
                {
                    // language is no longer installed, make the file empty. 
                    logger.LogDebug("Language in file is not on the site, cleaning");
                    var attempt = await serializer.SerializeEmptyAsync(item, SyncActionType.Delete, node.GetAlias());
                    if (attempt.Success && attempt.Item is not null)
                    {
                        await syncFileService.SaveXElementAsync(attempt.Item, file);
                    }
                }
            }
        }
    }

    private static ConcurrentDictionary<string, string> newLanguages = new();

    /// <inheritdoc/>
    public override async Task HandleAsync(SavingNotification<ILanguage> notification, CancellationToken cancellationToken)
    {
        if (_mutexService.IsPaused) return;

        if (await ShouldBlockRootChangesAsync(notification.SavedEntities))
        {
            notification.Cancel = true;
            notification.Messages.Add(GetCancelMessageForRoots());
            return;
        }

        foreach (var item in notification.SavedEntities)
        {
            // 
            if (item.Id == 0)
            {
                newLanguages.TryAdd(item.IsoCode, item.CultureName);
                // is new, we want to set this as a flag, so we don't do the full content save.n
                // newLanguages.Add(item.IsoCode);
            }
        }

        return;
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(SavedNotification<ILanguage> notification, CancellationToken cancellationToken)
    {
        if (_mutexService.IsPaused) return;

        foreach (var item in notification.SavedEntities)
        {
            bool newItem = false;

            if (newLanguages.TryRemove(item.IsoCode, out var _)) {
                newItem = true;
            }

            var targetFolders = GetDefaultHandlerFolders();

            if (item.WasPropertyDirty("IsDefault"))
            {
                // changing, this change doesn't trigger a save of the other languages.
                // so we need to save all language files. 
                await this.ExportAllAsync(targetFolders, DefaultConfig, null);
            }


            var attempts = await ExportAsync(item, targetFolders, DefaultConfig);

            if (!newItem && item.WasPropertyDirty(nameof(ILanguage.IsoCode)))
            {
                // The language code changed, this can mean we need to do a full content export. 
                // + we should export the languages again!
                uSyncTriggers.TriggerExport(targetFolders, 
                    [ UdiEntityType.Document, UdiEntityType.Language ], null);
            }

            // we always clean up languages, because of the way they are stored. 
            foreach (var attempt in attempts.Where(x => x.Success))
            {
                if (attempt.FileName is null) continue;

                await this.CleanUpAsync(item, attempt.FileName, targetFolders.Last());
            }

        }
    }

    protected override Task<IEnumerable<uSyncAction>> DeleteMissingItemsAsync(ILanguage parent, IEnumerable<Guid> keysToKeep, bool reportOnly)
        => Task.FromResult(Enumerable.Empty<uSyncAction>());
}
