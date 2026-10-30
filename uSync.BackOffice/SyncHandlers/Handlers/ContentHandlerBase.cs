using Microsoft.Extensions.Logging;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  base for all content based handlers
/// </summary>
/// <remarks>
///  Content based handlers can have the same name in different 
///  places around the tree, so we have to check for file name
///  clashes. 
/// </remarks>
public abstract class ContentHandlerBase<TObject> : SyncHandlerTreeBase<TObject>
    where TObject : IContentBase
{
    /// <summary>
    /// Base constructor, should never be called directly
    /// </summary>
    protected ContentHandlerBase(
        ILogger<ContentHandlerBase<TObject>> logger,
        IEntityService entityService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfigService,
        ISyncItemFactory syncItemFactory)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfigService, syncItemFactory)
    { }

    /// <summary>
    /// Generate a unique string based on the item name and path, used for matching to existing items
    /// </summary>
    protected override string GetItemMatchString(TObject item)
    {
        var itemPath = item.Level.ToString();
        if (item.Trashed && serializer is ISyncContentSerializer<TObject> contentSerializer)
        {
            itemPath = contentSerializer.GetItemPath(item);
        }
        return $"{item.Name}_{itemPath}";
    }

    /// <summary>
    ///  Generate a unique string based on the item name and path from a uSync xml file
    /// </summary>
    protected override string GetXmlMatchString(XElement node)
    {
        var path = node.Element("Info")?.Element("Path").ValueOrDefault(node.GetLevel().ToString());
        return $"{node.GetAlias()}_{path}";
    }


    /*
     *  Config options. 
     *    Include = Paths (comma separated) (only include if path starts with one of these)
     *    Exclude = Paths (comma separated) (exclude if path starts with one of these)
     *    
     *    RulesOnExport = bool (do we apply the rules on export as well as import?)
     */

    /// <summary>
    /// Should this item be imported (will check rules)
    /// </summary>
    protected override async Task<bool> ShouldImportAsync(XElement node, HandlerSettings config)
    {
        // check base first - if it says no - then no point checking this. 
        if (!await base.ShouldImportAsync(node, config)) return false;

        if (!ContentHandlerBase<TObject>.ShouldImportTrashedItem(node, config)) return false;

        if (!ImportPaths(node, config)) return false;

        if (!ByDocTypeConfigCheck(node, config)) return false;

        return true;
    }

    /// <summary>
    /// Import an item that is in the trashed state in the XML file.
    /// </summary>
    /// <remarks>
    /// Trashed items are only imported when the "ImportTrashed" setting is true on the handler
    /// </remarks>
    private static bool ShouldImportTrashedItem(XElement node, HandlerSettings config)
    {
        // unless the setting is explicit we don't import trashed items. 
        var trashed = node.Element("Info")?.Element("Trashed").ValueOrDefault(false);
        if (trashed.GetValueOrDefault(false) && !config.GetSetting("ImportTrashed", false)) return false;

        return true;
    }

    private bool ImportPaths(XElement node, HandlerSettings config)
    {
        var path = node.Element("Info")?.Element("Path").ValueOrDefault(string.Empty);
        if (string.IsNullOrWhiteSpace(path)) return true;

        if (IsIncludedPath(path, GetPathSetting(config, "Include")) is false)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Not processing item, {alias} path {path} not in include path", node.GetAlias(), path);

            return false;
        }

        if (IsExcludedPath(path, GetPathSetting(config, "Exclude")))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Not processing item, {alias} path {path} is excluded", node.GetAlias(), path);

            return false;
        }

        return true;
    }

    private static string[] GetPathSetting(HandlerSettings config, string key)
        => config.GetSetting(key, "").Split([','], StringSplitOptions.RemoveEmptyEntries);

    private static bool IsIncludedPath(string path, string[] include)
        => include.Length == 0 || include.Any(x => path.InvariantStartsWith(x));

    private static bool IsExcludedPath(string path, string[] exclude)
        => exclude.Any(x => path.InvariantStartsWith(x));

    /// <summary>
    ///  could anything at or below this path pass the Include and Exclude rules?
    /// </summary>
    /// <remarks>
    ///  everything below an item has a path that starts with the item's path, so a branch
    ///  can only hold an included item if an include path starts with the item's path (it's
    ///  further down the branch) or the item's path starts with an include path (it's in there already).
    /// </remarks>
    private static bool CanBranchHaveIncludedPaths(string path, string[] include, string[] exclude)
    {
        if (IsExcludedPath(path, exclude)) return false;
        return include.Length == 0
            || include.Any(x => path.InvariantStartsWith(x) || x.InvariantStartsWith(path));
    }

    private bool TryGetExportRulesPath(TObject item, HandlerSettings config, out string path)
    {
        path = string.Empty;
        if (config.GetSetting("RulesOnExport", false) is false) return false;
        if (serializer is not ISyncContentSerializer<TObject> contentSerializer) return false;

        path = contentSerializer.GetItemPath(item);
        return string.IsNullOrWhiteSpace(path) is false;
    }

    /// <summary>
    ///  skip items the Include and Exclude rules will not export (when RulesOnExport is on)
    /// </summary>
    protected override Task<bool> CanSkipExportAsync(TObject item, HandlerSettings config)
    {
        if (TryGetExportRulesPath(item, config, out var path) is false) return Task.FromResult(false);

        return Task.FromResult(
            IsIncludedPath(path, GetPathSetting(config, "Include")) is false
            || IsExcludedPath(path, GetPathSetting(config, "Exclude")));
    }

    /// <summary>
    ///  skip branches the Include and Exclude rules will not export anything from (when RulesOnExport is on)
    /// </summary>
    protected override Task<bool> CanSkipChildExportsAsync(TObject item, HandlerSettings config)
    {
        if (TryGetExportRulesPath(item, config, out var path) is false) return Task.FromResult(false);

        return Task.FromResult(
            CanBranchHaveIncludedPaths(path, GetPathSetting(config, "Include"), GetPathSetting(config, "Exclude")) is false);
    }

    private bool ByDocTypeConfigCheck(XElement node, HandlerSettings config)
    {
        var includeDocTypes = config.GetSetting("IncludeContentTypes", "").Split(',', StringSplitOptions.RemoveEmptyEntries);

        var doctype = node.Element("Info")?.Element("ContentType").ValueOrDefault(string.Empty);
        if (string.IsNullOrEmpty(doctype)) return true;

        if (includeDocTypes.Length > 0 && !includeDocTypes.InvariantContains(doctype))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Not processing {alias} as it in not in the Included by ContentType list {contentType}", node.GetAlias(), doctype);

            return false;
        }

        var excludeDocTypes = config.GetSetting("ExcludeContentTypes", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (excludeDocTypes.Length > 0 && excludeDocTypes.InvariantContains(doctype))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Not processing {alias} as it is excluded by ContentType {contentType}", node.GetAlias(), doctype);

            return false;
        }

        return true;
    }


    /// <summary>
    ///  Should we save this value to disk?
    /// </summary>
    /// <remarks>
    ///  In general we save everything to disk, even if we are not going to re-import it later
    ///  but you can stop this with RulesOnExport = true in the settings 
    /// </remarks>
    protected override async Task<bool> ShouldExportAsync(XElement node, HandlerSettings config)
    {
        if (!await base.ShouldExportAsync(node, config)) return false;

        // We export trashed items by default, (but we don't import them by default)
        var trashed = node.Element("Info")?.Element("Trashed").ValueOrDefault(false);
        if (trashed.GetValueOrDefault(false) && !config.GetSetting<bool>("ExportTrashed", true)) return false;

        if (config.GetSetting("RulesOnExport", false))
        {
            // we run the import rules (but not the base rules as that would confuse.)
            if (!ContentHandlerBase<TObject>.ShouldImportTrashedItem(node, config)) return false;
            if (!ImportPaths(node, config)) return false;
            if (!ByDocTypeConfigCheck(node, config)) return false;
        }

        return true;
    }


    // we only match duplicate actions by key. 

    /// <summary>
    /// Do the uSyncActions match by key (e.g are they the same item)
    /// </summary>
    protected override bool DoActionsMatch(uSyncAction a, uSyncAction b)
        => a.Key == b.Key;

    /// <summary>
    ///  Handle the Umbraco Moved to recycle bin notification, (treated like a move)
    /// </summary>
    public async Task HandleAsync(MovedToRecycleBinNotification<TObject> notification, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent()) return;
        await HandleMoveAsync(notification.MoveInfoCollection, cancellationToken);
    }

    /// <summary>
    ///  Check that roots isn't stopping an item from being recycled.
    /// </summary>
    public async Task HandleAsync(MovingToRecycleBinNotification<TObject> notification, CancellationToken cancellationToken)
    {
        if (await ShouldBlockRootChangesAsync(notification.MoveInfoCollection.Select(x => x.Entity)))
        {
            notification.Cancel = true;
            notification.Messages.Add(GetCancelMessageForRoots());
        }
    }

    /// <summary>
    ///  Clean up any files on disk, that might be left over when an item moves
    /// </summary>
    protected override async Task CleanUpAsync(TObject item, string newFile, string folder)
    {
        // for content this clean up check only catches when an item is moved from
        // one location to another, if the site is setup to useGuidNames and a flat 
        // structure that rename won't actually leave any old files on disk. 

        bool quickCleanup = this.DefaultConfig.GetSetting("QuickCleanup", false);
        if (quickCleanup)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Quick cleanup is on, so not looking in all config files");

            return;
        }

        // so we can skip this step and get a much quicker save process.
        if (this.DefaultConfig.GuidNames && this.DefaultConfig.UseFlatStructure) return;

        // check to see if we think this was a rename (so only do the clean up if we really have to)
        if (item.WasPropertyDirty(nameof(item.Name)) || item.WasPropertyDirty(nameof(item.ParentId)))
        {
            await base.CleanUpAsync(item, newFile, folder);
        }
    }

}
