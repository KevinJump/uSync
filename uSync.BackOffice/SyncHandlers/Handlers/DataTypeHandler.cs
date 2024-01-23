using System;
using System.Collections.Generic;
using System.Linq;
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
using uSync.Core;
using uSync.Core.Models;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers
{
    /// <summary>
    ///  Handler to manage DataTypes via uSync
    /// </summary>
    [SyncHandler(uSyncConstants.Handlers.DataTypeHandler, "Datatypes", "DataTypes", uSyncConstants.Priorites.DataTypes,
        Icon = "icon-autofill", IsTwoPass = true, EntityType = UdiEntityType.DataType)]
    public class DataTypeHandler : SyncHandlerContainerBase<IDataType, IDataTypeService>, ISyncHandler, ISyncPostImportHandler,
        INotificationHandler<SavedNotification<IDataType>>,
        INotificationHandler<MovedNotification<IDataType>>,
        INotificationHandler<DeletedNotification<IDataType>>,
        INotificationHandler<EntityContainerSavedNotification>,
        INotificationHandler<EntityContainerRenamedNotification>
    {
        private readonly IDataTypeService dataTypeService;

        /// <summary>
        /// Constructor called via DI
        /// </summary>
        public DataTypeHandler(
            ILogger<DataTypeHandler> logger,
            IEntityService entityService,
            IDataTypeService dataTypeService,
            AppCaches appCaches,
            IShortStringHelper shortStringHelper,
            SyncFileService syncFileService,
            uSyncEventService mutexService,
            uSyncConfigService uSyncConfig,
            ISyncItemFactory syncItemFactory)
            : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
        {
            this.dataTypeService = dataTypeService;
        }

        /// <summary>
        /// Process all DataType actions at the end of the import process
        /// </summary>
        /// <remarks>
        /// Datatypes have to exist early on so DocumentTypes can reference them, but
        /// some doctypes reference content or document types, so we re-process them
        /// at the end of the import process to ensure those settings can be made too.
        /// 
        /// HOWEVER: The above isn't a problem Umbraco 10+ - the references can be set
        /// before the actual doctypes exist, so we can do that in one pass.
        /// 
        /// HOWEVER: If we move deletes to the end , we still need to process them. 
        /// but deletes are always 'change' = 'Hidden', so we only process hidden changes
        /// </remarks>
        public override IEnumerable<uSyncAction> ProcessPostImport(IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            if (actions == null || !actions.Any())
                return Enumerable.Empty<uSyncAction>();

            var results = new List<uSyncAction>();          

            // we only do deletes here. 
            foreach (var action in actions.Where(x => x.Change == ChangeType.Hidden))
            {
                results.AddRange(
                    Import(action.FileName, config, SerializerFlags.LastPass));
            }

            results.AddRange(CleanFolders(-1));

            return results;
        }

        /// <summary>
        ///  Fetch a DataType Container from the DataTypeService
        /// </summary>
        protected override IEntity GetContainer(int id)
            => dataTypeService.GetContainer(id);

        /// <summary>
        ///  Fetch a DataType Container from the DataTypeService
        /// </summary>
        protected override IEntity GetContainer(Guid key)
            => dataTypeService.GetContainer(key);

        /// <summary>
        ///  Delete a DataType Container from the DataTypeService
        /// </summary>
        protected override void DeleteFolder(int id)
            => dataTypeService.DeleteContainer(id);

        /// <summary>
        ///  Get the filename to use for a DataType when we save it
        /// </summary>
        protected override string GetItemFileName(IDataType item)
            => GetItemAlias(item).ToSafeAlias(shortStringHelper);

        /// <inheritdoc />
        protected override SyncAttempt<XElement> Export_DoExport(IDataType item, string filename, string[] folders, HandlerSettings config)
        {
            // all the possible files that there could be. 
            var files = folders.Select(x => GetPath(x, item, config.GuidNames, config.UseFlatStructure)).ToArray();
            var nodes = syncFileService.GetAllNodes(files[..^1]);

            // with roots enabled - we attempt to merge doctypes ! 
            // 
            var attempt = SerializeItem(item, new Core.Serialization.SyncSerializerOptions(config.Settings));
            if (attempt.Success)
            {
                if (ShouldExport(attempt.Item, config))
                {
                    if (nodes.Count > 0)
                    {
                        nodes.Add(attempt.Item);
                        var difference = syncFileService.GetDifferences(nodes, trackers.FirstOrDefault());
                        if (difference != null)
                        {
                            syncFileService.SaveXElement(difference, filename);
                        }
                        else
                        {
                            if (syncFileService.FileExists(filename))
                                syncFileService.DeleteFile(filename);
                        }

                    }
                    else
                    {
                        syncFileService.SaveXElement(attempt.Item, filename);
                    }

                    if (config.CreateClean && HasChildren(item))
                        CreateCleanFile(GetItemKey(item), filename);
                }
                else
                {
                    return SyncAttempt<XElement>.Succeed(filename, ChangeType.NoChange, "Not Exported (Based on configuration)");
                }
            }

            return attempt;
        }
    }
}
