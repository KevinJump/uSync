﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Semver;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Extensions;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.Core;

namespace uSync.BackOffice
{

    /// <summary>
    ///  the service that does all the processing,
    ///  this forms the entry point as an API to 
    ///  uSync, it is where imports, exports and reports
    ///  are actually ran from. 
    /// </summary>
    public partial class uSyncService
    {
        /// <summary>
        ///  Callback event for SignalR hub
        /// </summary>
        public delegate void SyncEventCallback(SyncProgressSummary summary);

        private readonly ILogger<uSyncService> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly IEventAggregator _eventAggregator;

        private readonly uSyncConfigService _uSyncConfig;
        private readonly SyncHandlerFactory _handlerFactory;
        private SyncFileService _syncFileService;
        private readonly uSyncEventService _mutexService;

        private readonly ICoreScopeProvider _scopeProvider;

        private readonly IAppCache _appCache;

        /// <summary>
        ///  Create a new uSyncService (done via DI)
        /// </summary>
        public uSyncService(
            ILogger<uSyncService> logger,
            IEventAggregator eventAggregator,
            uSyncConfigService uSyncConfigService,
            SyncHandlerFactory handlerFactory,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            AppCaches appCaches,
            ICoreScopeProvider scopeProvider,
            ILoggerFactory loggerFactory)
        {
            this._logger = logger;

            this._eventAggregator = eventAggregator;

            this._uSyncConfig = uSyncConfigService;
            this._handlerFactory = handlerFactory;
            this._syncFileService = syncFileService;
            this._mutexService = mutexService;

            this._appCache = appCaches.RuntimeCache;

            uSyncTriggers.DoExport += USyncTriggers_DoExport;
            uSyncTriggers.DoImport += USyncTriggers_DoImport;
            _scopeProvider = scopeProvider;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        ///  Does the given folder contain and uSync files for Content
        /// </summary>
        public bool HasContentFiles(string rootFolder)
        {
            return _syncFileService.DirectoryExists(rootFolder + "Content");
        }


        #region Reporting 

        /// <summary>
        ///  Report the changes for a folder
        /// </summary>
        /// <param name="folder">Folder to run the report for</param>
        /// <param name="handlerOptions">Options to use for the report - used to load the handlers.</param>
        /// <param name="callbacks">Callback functions to keep UI upto date</param>
        /// <returns>List of actions detailing what would and wouldn't change</returns>
        public IEnumerable<uSyncAction> Report(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Report;

            var handlers = _handlerFactory.GetValidHandlers(handlerOptions);
            return Report(folder, handlers, callbacks);
        }

        /// <summary>
        ///  Report the changes for a folder
        /// </summary>
        /// <param name="folder">Folder to run the report for</param>
        /// <param name="handlerAliases">List of Aliases for the sync handlers to use</param>
        /// <param name="callbacks">Callback functions to keep UI upto date</param>
        /// <returns>List of actions detailing what would and wouldn't change</returns>
        public IEnumerable<uSyncAction> Report(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        {
            var handlers = _handlerFactory.GetDefaultHandlers(handlerAliases);
            return Report(folder, handlers, callbacks);
        }

        /// <summary>
        ///  Report the changes for a folder
        /// </summary>
        /// <param name="folder">Folder to run the report for</param>
        /// <param name="handlers">List of SyncHandlers to use for the report</param>
        /// <param name="callbacks">Callback functions to keep UI upto date</param>
        /// <returns>List of actions detailing what would and wouldn't change</returns>
        public IEnumerable<uSyncAction> Report(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {

            var sw = Stopwatch.StartNew();

            _mutexService.FireBulkStarting(new uSyncReportStartingNotification());

            _logger.LogDebug("Reporting For [{handlers}]", string.Join(",", handlers.Select(x => x.Handler.Name)));

            var actions = new List<uSyncAction>();

            var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Reporting", handlers.Count());

            foreach (var configuredHandler in handlers)
            {
                var handler = configuredHandler.Handler;
                var handlerSettings = configuredHandler.Settings;

                summary.Increment();

                summary.UpdateHandler(handler.Name, HandlerStatus.Processing, $"Reporting {handler.Name}", 0);

                callbacks?.Callback?.Invoke(summary);

                var handlerActions = handler.Report($"{folder}/{handler.DefaultFolder}", handlerSettings, callbacks?.Update);
                actions.AddRange(handlerActions);

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete,
                    handlerActions.CountChanges(),
                    handlerActions.ContainsErrors());
            }

            summary.UpdateMessage("Report Complete");
            callbacks?.Callback?.Invoke(summary);


            _mutexService.FireBulkComplete(new uSyncReportCompletedNotification(actions));
            sw.Stop();

            _logger.LogInformation("uSync Report: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
                handlers.Count(), actions.Count,
                actions.CountChanges(),
                sw.ElapsedMilliseconds);

            callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

            return actions;
        }

        #endregion

        #region Importing
        private static object _importLock = new object();

        /// <summary>
        ///  Import items into Umbraco from a given folder
        /// </summary>
        /// <param name="folder">Folder to use for the import</param>
        /// <param name="force">Push changes in even if there is no difference between the file and the item in Umbraco</param>
        /// <param name="handlerOptions">Handler options to use (used to calculate handlers to use)</param>
        /// <param name="callbacks">Callbacks to keep UI informed</param>
        /// <returns>List of actions detailing what did and didn't change</returns>
        public IEnumerable<uSyncAction> Import(string folder, bool force, SyncHandlerOptions handlerOptions, uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Import;

            var handlers = _handlerFactory.GetValidHandlers(handlerOptions);
            return Import(folder, force, handlers, callbacks);
        }

        /// <summary>
        ///  Import items into Umbraco from a given folder
        /// </summary>
        /// <param name="folder">Folder to use for the import</param>
        /// <param name="force">Push changes in even if there is no difference between the file and the item in Umbraco</param>
        /// <param name="handlerAliases">List of aliases for the handlers you want to use</param>
        /// <param name="callbacks">Callbacks to keep UI informed</param>
        /// <returns>List of actions detailing what did and didn't change</returns>
        public IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        {
            var handlers = _handlerFactory.GetDefaultHandlers(handlerAliases);
            return Import(folder, force, handlers, callbacks);
        }

        /// <summary>
        ///  Import items into Umbraco from a given folder
        /// </summary>
        /// <param name="folder">Folder to use for the import</param>
        /// <param name="force">Push changes in even if there is no difference between the file and the item in Umbraco</param>
        /// <param name="handlers">List of Handlers &amp; config to use for import</param>
        /// <param name="callbacks">Callbacks to keep UI informed</param>
        /// <returns>List of actions detailing what did and didn't change</returns>
        public IEnumerable<uSyncAction> Import(string folder, bool force, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {
            // if its blank, we just throw it back empty. 
            if (handlers == null || !handlers.Any()) return Enumerable.Empty<uSyncAction>();

            lock (_importLock)
            {
                var sw = Stopwatch.StartNew();

                using (var pause = _mutexService.ImportPause(true))
                {

                    // pre import event
                    _mutexService.FireBulkStarting(new uSyncImportStartingNotification());

                    var actions = new List<uSyncAction>();

                    var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Importing", handlers.Count() + 1);
                    summary.Handlers.Add(new SyncHandlerSummary()
                    {
                        Icon = "icon-defrag",
                        Name = "Post Import",
                        Status = HandlerStatus.Pending
                    });

                    foreach (var configuredHandler in handlers)
                    {
                        var handler = configuredHandler.Handler;
                        var handlerSettings = configuredHandler.Settings;

                        summary.Increment();

                        summary.UpdateHandler(
                            handler.Name, HandlerStatus.Processing, $"Importing {handler.Name}", 0);

                        callbacks?.Callback?.Invoke(summary);

                        var handlerActions = handler.ImportAll($"{folder}/{handler.DefaultFolder}", handlerSettings, force, callbacks?.Update);
                        actions.AddRange(handlerActions);

                        summary.UpdateHandler(handler.Name, HandlerStatus.Complete,
                            handlerActions.CountChanges(),
                            handlerActions.ContainsErrors());

                    }


                    // postImport things (mainly cleaning up folders)

                    summary.Increment();
                    summary.UpdateHandler("Post Import", HandlerStatus.Pending, "Post Import Actions", 0);

                    callbacks?.Callback?.Invoke(summary);

                    actions.AddRange(PerformPostImport(folder, handlers, actions));

                    sw.Stop();
                    summary.UpdateHandler("Post Import", HandlerStatus.Complete, "Import Completed", 0);
                    callbacks?.Callback?.Invoke(summary);

                    // fire complete
                    _mutexService.FireBulkComplete(new uSyncImportCompletedNotification(actions));

                    _logger.LogInformation("uSync Import: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
                        handlers.Count(),
                        actions.Count,
                        actions.CountChanges(),
                        sw.ElapsedMilliseconds);

                    callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

                    return actions;
                }
            }
        }

        private IEnumerable<uSyncAction> PerformPostImport(string rootFolder, IEnumerable<HandlerConfigPair> handlers, IEnumerable<uSyncAction> actions)
        {
            var postImportActions = actions.Where(x => x.Success && x.Change > Core.ChangeType.NoChange && x.RequiresPostProcessing).ToList();
            if (postImportActions.Count == 0) return Enumerable.Empty<uSyncAction>();

            var results = new List<uSyncAction>();

            foreach (var handlerPair in handlers)
            {
                if (handlerPair.Handler is ISyncPostImportHandler postImportHandler)
                {
                    var handlerActions = postImportActions.Where(x => x.ItemType == handlerPair.Handler.ItemType);

                    if (handlerActions.Any())
                    {
                        var handlerFolder = GetHandlerFolder(rootFolder, handlerPair.Handler);
                        var postActions = postImportHandler.ProcessPostImport(handlerFolder, handlerActions, handlerPair.Settings);
                        if (postActions != null)
                            results.AddRange(postActions);
                    }
                }
            }

            return results;
        }


        /// <summary>
        ///  Import a single item based on a uSyncAction item
        /// </summary>
        /// <remarks>
        ///  Importing a single item based on an action, the action will
        ///  detail what handler to use and the filename of the item to import
        /// </remarks>
        /// <param name="action">Action item, to use as basis for import</param>
        /// <returns>Action detailing change or not</returns>
        public uSyncAction ImportSingleAction(uSyncAction action)
        {
            var handlerConfig = _handlerFactory.GetValidHandler(action.HandlerAlias);

            if (handlerConfig != null)
            {
                return handlerConfig.Handler
                    .Import(action.FileName, handlerConfig.Settings, true)
                    .FirstOrDefault();
            }

            return new uSyncAction();

        }

        #endregion

        #region Exporting 

        /// <summary>
        ///  Remove all the files from an export folder 
        /// </summary>
        public bool CleanExportFolder(string folder)
        {
            try
            {
                if (_syncFileService.DirectoryExists(folder))
                    _syncFileService.CleanFolder(folder);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to delete uSync folder (may be in use)", ex);
            }

            return true;
        }


        /// <summary>
        ///  Export items from Umbraco into a given folder
        /// </summary>
        /// <param name="folder">folder to place items</param>
        /// <param name="handlerOptions">Handler options to use when loading handlers</param>
        /// <param name="callbacks">callback functions to update the UI</param>
        /// <returns>List of actions detailing what was exported</returns>
        public IEnumerable<uSyncAction> Export(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks callbacks = null)
        {
            if (handlerOptions == null) handlerOptions = new SyncHandlerOptions();
            handlerOptions.Action = HandlerActions.Export;

            var handlers = _handlerFactory.GetValidHandlers(handlerOptions);

            WriteVersionFile(folder);

            return Export(folder, handlers, callbacks);
        }

        /// <summary>
        ///  Check the uSync version file (in the root) to see if we are importing upto date files
        /// </summary>
        public bool CheckVersionFile(string folder)
        {
            var versionFile = Path.Combine(_syncFileService.GetAbsPath(folder), $"usync.{_uSyncConfig.Settings.DefaultExtension}");

            if (!_syncFileService.FileExists(versionFile))
            {
                return false;
            }
            else
            {
                try
                {
                    var node = _syncFileService.LoadXElement(versionFile);
                    var format = node.Attribute("format").ValueOrDefault("");
                    if (!format.InvariantEquals(Core.uSyncConstants.FormatVersion))
                    {
                        var expectedVersion = SemVersion.Parse(Core.uSyncConstants.FormatVersion);
                        if (SemVersion.TryParse(format, out SemVersion current))
                        {
                            if (current.CompareTo(expectedVersion) >= 0) return true;
                        }

                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private void WriteVersionFile(string folder)
        {
            try
            {
                var versionFile = Path.Combine(_syncFileService.GetAbsPath(folder), $"usync.{_uSyncConfig.Settings.DefaultExtension}");
                var versionNode = new XElement("uSync",
                    new XAttribute("version", typeof(uSync).Assembly.GetName().Version.ToString()),
                    new XAttribute("format", Core.uSyncConstants.FormatVersion));
                // remove date, we don't really care, and it causes unnecessary git changes.

                Directory.CreateDirectory(Path.GetDirectoryName(versionFile));

                versionNode.Save(versionFile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Issue saving the usync.conifg file in the root of {folder}", folder);
            }
        }

        /// <summary>
        ///  Export items from Umbraco into a given folder
        /// </summary>
        /// <param name="folder">folder to place items</param>
        /// <param name="handlerAliases">aliases for the handlers to use while exporting</param>
        /// <param name="callbacks">callback functions to update the UI</param>
        /// <returns>List of actions detailing what was exported</returns>
        public IEnumerable<uSyncAction> Export(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks callbacks)
        {
            var handlers = _handlerFactory.GetDefaultHandlers(handlerAliases);
            return Export(folder, handlers, callbacks);
        }

        /// <summary>
        ///  Export items from Umbraco into a given folder
        /// </summary>
        /// <param name="folder">folder to place items</param>
        /// <param name="handlers">Handler config pairs</param>
        /// <param name="callbacks">callback functions to update the UI</param>
        /// <returns>List of actions detailing what was exported</returns>
        public IEnumerable<uSyncAction> Export(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks callbacks)
        {
            var sw = Stopwatch.StartNew();

            _mutexService.FireBulkStarting(new uSyncExportStartingNotification());

            var actions = new List<uSyncAction>();
            var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Exporting", handlers.Count());

            foreach (var configuredHandler in handlers)
            {
                var handler = configuredHandler.Handler;

                summary.Increment();
                summary.UpdateHandler(
                    handler.Name, HandlerStatus.Processing, $"Exporting {handler.Name}", 0);

                callbacks?.Callback?.Invoke(summary);

                var handlerActions = handler.ExportAll($"{folder}/{handler.DefaultFolder}", configuredHandler.Settings, callbacks?.Update);

                actions.AddRange(handlerActions);

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete,
                    handlerActions.CountChanges(),
                    handlerActions.ContainsErrors());
            }


            summary.UpdateMessage("Export Completed");
            callbacks?.Callback?.Invoke(summary);

            _mutexService.FireBulkComplete(new uSyncExportCompletedNotification(actions));

            sw.Stop();

            _logger.LogInformation("uSync Export: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
                handlers.Count(), actions.Count,
                actions.CountChanges(),
                sw.ElapsedMilliseconds);

            callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

            return actions;
        }

        #endregion

        /// <summary>
        ///  Do an import triggered by an event.
        /// </summary>
        /// <param name="e"></param>
        private void USyncTriggers_DoImport(uSyncTriggerArgs e)
        {
            if (e.EntityTypes != null && !string.IsNullOrWhiteSpace(e.Folder))
            {
                _logger.LogInformation("Import Triggered by downlevel change {folder}", e.Folder);

                var handlers = _handlerFactory
                    .GetValidHandlersByEntityType(e.EntityTypes, e.HandlerOptions);

                if (handlers.Any()) this.Import(e.Folder, false, handlers, null);
            }
        }

        /// <summary>
        ///  do an export triggered by events. 
        /// </summary>
        /// <param name="e"></param>
        private void USyncTriggers_DoExport(uSyncTriggerArgs e)
        {
            if (e.EntityTypes != null && !string.IsNullOrWhiteSpace(e.Folder))
            {
                _logger.LogInformation("Export Triggered by downlevel change {folder}", e.Folder);

                var handlers = _handlerFactory
                    .GetValidHandlersByEntityType(e.EntityTypes, e.HandlerOptions);

                if (handlers.Any()) this.Export(e.Folder, handlers, null);
            }
        }
    }
}
