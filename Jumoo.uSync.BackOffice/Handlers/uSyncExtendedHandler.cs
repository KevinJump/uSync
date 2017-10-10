using System;
using System.IO;
using System.Xml.Linq;
using Jumoo.uSync.BackOffice.Helpers;
using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Extensions;
using Jumoo.uSync.Core.Interfaces;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.EntityBase;

namespace Jumoo.uSync.BackOffice.Handlers
{
    public abstract class uSyncExtendedHandler<T> : uSyncBaseHandler<T>
           where T : IEntity
    {
        protected uSyncExtendedHandler(ISyncExtendedSerializer<T> serializer)
        {
            Serializer = serializer;
        }

        public abstract string SyncFolder { get; }

        public ISyncExtendedSerializer<T> Serializer { get; }

        public override SyncAttempt<T> Import(string filePath, bool force = false)
        {
            LogHelper.Debug<uSyncActionHelper<T>>("Importing {0} : {1}", () => typeof(T), () => filePath);
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var node = XElement.Load(filePath);
            return Serializer.Deserialize(node, force, false);
        }


        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var uSyncAction = uSyncActionHelper<T>.ReportAction(Serializer.IsUpdate(node), node.NameFromNode());
            if (uSyncAction.Change > ChangeType.NoChange)
                uSyncAction.Details = Serializer.GetChanges(node);
            return uSyncAction;
        }

        public override void ImportSecondPass(string file, T item)
        {
            if (!File.Exists(file)) throw new FileNotFoundException();
            var node = XElement.Load(file);
            Serializer.DesearlizeSecondPass(item, node);
        }

        protected uSyncAction Export(T item, string itemPath, string folder)
        {
            if (item == null) return uSyncAction.Fail(Path.GetFileName(itemPath), typeof(T), $"{typeof(T)} is not set");

            try
            {
                var attempt = Serializer.Serialize(item);

                var filename = string.Empty;
                if (!attempt.Success) return uSyncActionHelper<XElement>.SetAction(attempt, filename);
                filename = uSyncIOHelper.SavePath(folder, SyncFolder, itemPath, uSyncConstants.Xml.FileName);
                uSyncIOHelper.SaveNode(attempt.Item, filename);

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);
            }
            catch (Exception ex)
            {
                LogHelper.Warn<uSyncExtendedHandler<T>>($"Error saving {typeof(T)}: {0}", () => ex);
                return uSyncAction.Fail(GetItemName(item), typeof(T), ChangeType.Export, ex);
            }
        }


        protected void DeleteFile(T item, bool addDeleteAction = true)
        {
            LogHelper.Info<uSyncExtendedHandler<T>>("Delete: Remove usync files for {0}", () => GetItemName(item));
            uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetItemPath(item), uSyncConstants.Xml.FileName);
            if (!addDeleteAction) return;
            uSyncBackOfficeContext.Instance.Tracker.AddAction(SyncActionType.Delete, item.Key,
                GetItemName(item), typeof(T));
        }

        public abstract String GetItemName(T item);
    }
}