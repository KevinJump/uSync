using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using uSync8.BackOffice.Hubs;
using uSync8.BackOffice.SyncHandlers;
using Constants = Umbraco.Core.Constants;

namespace uSync8.BackOffice.Controllers
{
    [PluginController("uSync")]
    [UmbracoApplicationAuthorize(Constants.Applications.Settings)]
    public class uSyncDashboardApiController : UmbracoAuthorizedApiController
    {
        private readonly uSyncBackOfficeSettings settings;
        private readonly SyncHandlerCollection syncHandlers;

        public uSyncDashboardApiController(
            SyncHandlerCollection syncHandlers,
            uSyncBackOfficeSettings settings)
        {
            this.settings = settings;
            this.syncHandlers = syncHandlers;
        }

        [HttpGet]
        public uSyncBackOfficeSettings GetSettings()
        {
            return settings;
        }

        [HttpGet]
        public IEnumerable<object> GetHandlers()
        {
            return syncHandlers.Select(x => new
            {
                Name = x.Name,
                Enabled = x.Enabled,
                Icon = x.Icon,
                Type = x.GetType(),
                Folder = x.DefaultFolder,
                Priority = x.Priority
            }).OrderBy(x => x.Priority);
        }


        [HttpPost]
        public IEnumerable<uSyncAction> Report(uSyncOptions options)
        {
            var updates = new List<uSyncAction>();

            var hubClient = new HubClientService(options.clientId);
            var handlers = syncHandlers.Where(x => x.Enabled).ToList();

            var progress = new Progress(handlers, "reporting");
            var complete = 0;

            foreach (var syncHandler in handlers)
            {
                complete++;
                progress.Message = $"checking {syncHandler.Name}";
                progress.UpdateHandler(syncHandler.Name, HandlerStatus.Processing);
                hubClient.SendMessage(progress);

                updates.AddRange(syncHandler.Report(syncHandler.DefaultFolder));

                progress.UpdateHandler(syncHandler.Name, HandlerStatus.Complete);
                progress.Percent = (int)Math.Round((double)(100 * complete) / handlers.Count);
            }

            return updates;
        }

        [HttpPost]
        public IEnumerable<uSyncAction> Export(uSyncOptions options)
        {
            var updates = new List<uSyncAction>();

            var hubClient = new HubClientService(options.clientId);
            var handlers = syncHandlers.Where(x => x.Enabled).ToList();

            var progress = new Progress(handlers, "Exporting");
            var complete = 0;

            foreach (var syncHandler in handlers)
            {
                complete++;
                progress.Message = $"Exporting {syncHandler.Name}";
                progress.UpdateHandler(syncHandler.Name, HandlerStatus.Processing);
                hubClient.SendMessage(progress);

                updates.AddRange(syncHandler.ExportAll(syncHandler.DefaultFolder));

                progress.UpdateHandler(syncHandler.Name, HandlerStatus.Complete);
                progress.Percent = (int)Math.Round((double)(100 * complete) / handlers.Count);
            }

            return updates;
        }

        [HttpPut]
        public IEnumerable<uSyncAction> Import(uSyncOptions options)
        {
            var updates = new List<uSyncAction>();

            var hubClient = new HubClientService(options.clientId);
            var handlers = syncHandlers.Where(x => x.Enabled).ToList();

            var progress = new Progress(handlers, "Importing");
            var complete = 0;

            foreach (var syncHandler in syncHandlers.Where(x => x.Enabled))
            {
                complete++;
                progress.Message = $"Importing {syncHandler.Name}";
                progress.UpdateHandler(syncHandler.Name, HandlerStatus.Processing);
                hubClient.SendMessage(progress);

                updates.AddRange(syncHandler.ImportAll(syncHandler.Name, options.force));

                progress.UpdateHandler(syncHandler.Name, HandlerStatus.Complete);
                progress.Percent = (int)Math.Round((double)(100 * complete) / handlers.Count);
            }

            return updates;
        }

        public class Progress
        {
            public Progress(IEnumerable<ISyncHandler> handlers, string message)
            {
                Percent = 0;
                Message = message;
                Handlers = handlers.Select(x => new HandlerInfo()
                {
                    Icon = x.Icon,
                    Name = x.Name,
                    Status = HandlerStatus.Pending
                }).ToList();
            }

            public void UpdateHandler(string name, HandlerStatus status)
            {
                var info = Handlers.FirstOrDefault(x => x.Name == name);
                if (info != null)
                    info.Status = status;
            }

            public int Percent { get; set; }
            public string Message { get; set; }
            public List<HandlerInfo> Handlers { get; set; }
        }

        public class HandlerInfo
        {
            public string Icon { get; set; }
            public string Name { get; set; }
            public HandlerStatus Status { get; set; }
        }

        public enum HandlerStatus
        {
            Pending, Processing, Complete, Error
        }

        public class uSyncOptions
        {
            public string clientId { get; set; }
            public bool force { get; set; }
            public bool clean { get; set; }
        }
    }
}
