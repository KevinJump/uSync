

namespace Jumoo.uSync.BackOffice.Handlers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using System.Collections.Generic;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;
    using Umbraco.Core.Logging;

    using Jumoo.uSync.Core;
    using Jumoo.uSync.BackOffice.Helpers;
    using Core.Extensions;
    using Umbraco.Core.Models.EntityBase;
    using System.Timers;

    public class DataTypeHandler : uSyncBaseHandler<IDataTypeDefinition>, ISyncHandler, ISyncPostImportHandler
    {
        public string Name { get { return "uSync: DataTypeHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.DataTypes; } }
        public string SyncFolder { get { return Constants.Packaging.DataTypeNodeName; } }

        readonly IDataTypeService _dataTypeService;
        readonly IEntityService _entityService;

        public DataTypeHandler()
        {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
            _entityService = ApplicationContext.Current.Services.EntityService;

            RequiresPostProcessing = true;
        }

        public override SyncAttempt<IDataTypeDefinition> Import(string filePath, bool force = false)
        {
            LogHelper.Debug<IDataTypeDefinition>(">> Import: {0}", () => filePath);

            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var node = XElement.Load(filePath);
            node = UnwrapPreValueFromCDataSection(node);

            return uSyncCoreContext.Instance.DataTypeSerializer.Deserialize(node, force, false);
        }

        public override uSyncAction DeleteItem(Guid key, string keyString)
        {
            IDataTypeDefinition item = null;
            if (key != Guid.Empty)
                item = _dataTypeService.GetDataTypeDefinitionById(key);

            if (item != null)
            {
                LogHelper.Info<DataTypeHandler>("Deleting datatype: {0}", () => item.Name);
                _dataTypeService.Delete(item);
                return uSyncAction.SetAction(true, keyString, typeof(IDataTypeDefinition), ChangeType.Delete, "Removed");
            }

            return uSyncAction.Fail(keyString, typeof(IDataTypeDefinition), ChangeType.Delete, "Not found");
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<DataTypeHandler>("Exporting all DataTypes.");

            return Export(-1, folder);
        }

        /// <summary>
        ///  v7.4 - we have folders - when we have folders we need to look for containers.
        /// </summary>
        public IEnumerable<uSyncAction> Export(int parent, string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var folders = _entityService.GetChildren(parent, UmbracoObjectTypes.DataTypeContainer);
            foreach (var fldr in folders)
            {
                actions.AddRange(Export(fldr.Id, folder));
            }

            var nodes = ApplicationContext.Current.Services.EntityService.GetChildren(parent, UmbracoObjectTypes.DataType);
            foreach (var node in nodes)
            {
                var item = _dataTypeService.GetDataTypeDefinitionById(node.Key);
                actions.Add(ExportToDisk(item, folder));

                actions.AddRange(Export(node.Id, folder));
            }

            return actions;
        }


        public uSyncAction ExportToDisk(IDataTypeDefinition item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(IDataTypeDefinition), "item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.DataTypeSerializer.Serialize(item);
                var filename = string.Empty;

                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(folder, SyncFolder, GetItemPath(item), item.Name.ToSafeAlias());
                    var wrappedItem = WrapPreValueInCDataSection(attempt.Item);
                    uSyncIOHelper.SaveNode(wrappedItem, filename);
                }

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);
            }
            catch (Exception ex)
            {
                LogHelper.Warn<DataTypeHandler>("Failed to save {0} - {1}", ()=> item.Name, () => ex.ToString());
                return uSyncAction.Fail(item.Name, item.GetType(), ChangeType.Export, ex);
            }
        }

        private static Timer _saveTimer;
        private static Queue<int> _saveQueue;
        private static object _saveLock;

        public void RegisterEvents()
        {
            DataTypeService.Saved += DataTypeService_Saved;
            DataTypeService.Deleted += DataTypeService_Deleted;
            DataTypeService.Moved += DataTypeService_Moved;
            DataTypeService.SavedContainer += DataTypeService_SavedContainer;

            // delay trigger - used (upto and including umb 7.4.2
            // saved event on a datatype is called before prevalues
            // are saved - so we just wait a little while before 
            // we save our datatype... 
            //  not ideal but them's the breaks.
            //
            //
            //
            _saveTimer = new Timer(4064); // 1/2 a perfect wait.
            _saveTimer.Elapsed += _saveTimer_Elapsed;

            _saveQueue = new Queue<int>();
            _saveLock = new object();
        }

        private void _saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock( _saveLock)
            {
                while (_saveQueue.Count > 0 )
                {
                    int id = _saveQueue.Dequeue();

                    var item = _dataTypeService.GetDataTypeDefinitionById(id);
                    if (item != null)
                    {
                        SaveToDisk(item);
                    }
                }
            }
        }



        private void DataTypeService_SavedContainer(IDataTypeService sender, Umbraco.Core.Events.SaveEventArgs<EntityContainer> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                Export(item.Id, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            }
        }

        private void DataTypeService_Deleted(IDataTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IDataTypeDefinition> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<DataTypeHandler>("Delete: Deleting uSync File for item: {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetItemPath(item), item.Name.ToSafeAlias());

                uSyncBackOfficeContext.Instance.Tracker.AddAction(SyncActionType.Delete, item.Key, item.Name, typeof(IDataTypeDefinition));
            }
        }


        private void DataTypeService_Saved(IDataTypeService sender, Umbraco.Core.Events.SaveEventArgs<IDataTypeDefinition> e)
        {
            if (uSyncEvents.Paused)
                return;

            lock (_saveLock)
            {
                _saveTimer.Stop();
                _saveTimer.Start();
                foreach (var item in e.SavedEntities)
                {
                    _saveQueue.Enqueue(item.Id);
                }
            }
        }

        private void DataTypeService_Moved(IDataTypeService sender, Umbraco.Core.Events.MoveEventArgs<IDataTypeDefinition> e)
        {
            if (uSyncEvents.Paused)
                return;

            lock(_saveLock)
            {
                _saveTimer.Stop();
                _saveTimer.Start();
                foreach(var item in e.MoveInfoCollection)
                {
                    _saveQueue.Enqueue(item.Entity.Id);
                }
            }
        }

        private void SaveToDisk(IDataTypeDefinition item)
        {
            LogHelper.Info<DataTypeHandler>("Save: Saving uSync file for item: {0}", () => item.Name);
            var action = ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            if (action.Success)
            {
                NameChecker.ManageOrphanFiles(SyncFolder, item.Key, action.FileName);
            }
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);

            var update = uSyncCoreContext.Instance.DataTypeSerializer.IsUpdate(node);
            var action = uSyncActionHelper<IDataTypeDefinition>.ReportAction(update, node.NameFromNode());
            if (action.Change > ChangeType.NoChange)
                action.Details = ((ISyncChangeDetail)uSyncCoreContext.Instance.DataTypeSerializer).GetChanges(node);
            return action;

        }

        public IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions)
        {
            if (actions == null || !actions.Any())
                return null;
            
            // we get passed actions that need a second pass.
            var datatypes = actions.Where(x => x.ItemType == typeof(IDataTypeDefinition));
            if (datatypes == null || !datatypes.Any())
                return null;

            foreach (var action in datatypes)
            {
                LogHelper.Debug<DataTypeHandler>("Post Processing: {0} {1}", () => action.Name, () => action.FileName);
                var attempt = Import(action.FileName);
                if (attempt.Success)
                {
                    ImportSecondPass(action.FileName, attempt.Item);
                }
            }

            return CleanEmptyContainers(folder, -1);
        }

        private IEnumerable<uSyncAction> CleanEmptyContainers(string folder, int parentId)
        {
            var actions = new List<uSyncAction>();

            var folders = _entityService.GetChildren(parentId, UmbracoObjectTypes.DataTypeContainer).ToArray();
            foreach (var fldr in folders)
            {
                actions.AddRange(CleanEmptyContainers(folder, fldr.Id));

                if (!_entityService.GetChildren(fldr.Id).Any())
                {
                    actions.Add(uSyncAction.SetAction(true, fldr.Name, typeof(EntityContainer), ChangeType.Delete, "Empty Container"));
                    _dataTypeService.DeleteContainer(fldr.Id);
                }
            }

            return actions;
        }

        #region PreValue Value CData wrapper and Unwrapper

        private static XElement WrapPreValueInCDataSection(XElement node)
        {
            var preValueRoot = node.Element("PreValues");
            if (preValueRoot.HasElements)
            {
                var preValues = preValueRoot.Elements("PreValue");
                foreach (var preValue in preValues)
                {
                    var value = preValue.Attribute("Value");
                    var cdata = new XCData(value.Value);
                    preValue.Add(cdata);
                    value.Remove();
                }
            }
            return node;
        }

        private static XElement UnwrapPreValueFromCDataSection(XElement node)
        {
             var preValueRoot = node.Element("PreValues");
            if (preValueRoot.HasElements)
            {
                var preValues = preValueRoot.Elements("PreValue");
                foreach (var preValue in preValues)
                {
                    if (preValue.Attribute("Value") == null)
                    {
                        var cdata = preValue.FirstNode as XCData;
                        if (cdata != null)
                        {
                            var attribute = new XAttribute("Value", cdata.Value);
                            preValue.Add(attribute);
                            cdata.Remove();
                        }
                    }
                }
            
            }
            

            return node;
        }

        #endregion
    }
}
