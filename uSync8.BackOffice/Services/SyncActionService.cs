using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using uSync8.BackOffice.Models;

namespace uSync8.BackOffice.Services
{
    public class SyncActionService
    {
        private readonly SyncFileService fileService;
        private readonly string actionFile;
        private List<SyncAction> actions = new List<SyncAction>();

        public SyncActionService(SyncFileService syncFileService, string file)
        {
            this.fileService = syncFileService;
            this.actionFile = file;
        }

        public IEnumerable<SyncAction> GetActions()
        {
            this.actions = new List<SyncAction>();
            if (fileService.FileExists(actionFile))
                this.actions = fileService.LoadXml<List<SyncAction>>(actionFile);

            return this.actions;
        }

        public void SaveActions()
        {
            fileService.SaveXml(actionFile, actions);
        }

        public void AddAction(Guid key, string alias, SyncActionType actionType)
        {
            if (!actions.Any(x => x.Key == key
                && x.Alias == alias))
            {
                actions.Add(new SyncAction()
                {
                    Key = key,
                    Alias = alias,
                    Action = actionType
                });
            }
        }

        public void AddAction(Guid key, SyncActionType actionType)
        {
            AddAction(key, "", actionType);
        }

        public void AddAction(string alias, SyncActionType actionType)
        {
            AddAction(Guid.NewGuid(), alias, actionType);
        }

        public void RemoveActions(Guid key)
        {
            actions.RemoveAll(x => x.Key == key);
        }

        public void RemoveActions(string alias)
        {
            actions.RemoveAll(x => x.Alias == alias);
        }

        public void RemoveActions(Guid key, string alias)
        {
            actions.RemoveAll(x => x.Key == key && x.Alias == alias);
        }
    }
        
}
