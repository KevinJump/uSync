using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.Core.Serialization;
using uSync8.Core.Tracking;

namespace uSync8.BackOffice.SyncHandlers
{
    /// <summary>
    ///  Base hanlder for objects that have container based trees
    /// </summary>
    /// <remarks>
    /// <para>
    /// In container based trees all items have unique aliases 
    /// across the whole tree. 
    /// </para>
    /// <para>
    /// you can't for example have two doctypes with the same 
    /// alias in diffrent containers. 
    /// </para>
    /// </remarks>
    public abstract class SyncHandlerContainerBase<TObject, TService>
        : SyncHandlerTreeBase<TObject, TService>
        where TObject : ITreeEntity
        where TService : IService
    {
        protected SyncHandlerContainerBase(IEntityService entityService, IProfilingLogger logger, ISyncSerializer<TObject> serializer, ISyncTracker<TObject> tracker, SyncFileService syncFileService)
            : base(entityService, logger, serializer, tracker, syncFileService)
        {
        }

        protected IEnumerable<uSyncAction> CleanFolders(string folder, int parent)
        {
            var actions = new List<uSyncAction>();

            var folders = entityService.GetChildren(parent, this.itemContainerType);
            foreach (var fdlr in folders)
            {
                actions.AddRange(CleanFolders(folder, fdlr.Id));

                if (!entityService.GetChildren(fdlr.Id).Any())
                {
                    actions.Add(uSyncAction.SetAction(true, fdlr.Name, typeof(EntityContainer), ChangeType.Delete, "Empty Container"));
                    DeleteFolder(fdlr.Id);
                }
            }

            return actions;
        }

        abstract protected void DeleteFolder(int id);

        public virtual IEnumerable<uSyncAction> ProcessPostImport(string folder, IEnumerable<uSyncAction> actions, HandlerSettings config)
        {
            if (actions == null || !actions.Any())
                return null;

            return CleanFolders(folder, -1);
        }

    }
}
