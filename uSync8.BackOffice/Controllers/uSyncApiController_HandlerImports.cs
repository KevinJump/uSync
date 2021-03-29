using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

using Umbraco.Web.WebApi;

using uSync8.BackOffice.Hubs;
using uSync8.BackOffice.Models;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice.Controllers
{
    /// <summary>
    ///  Actions by handler, allows for things to be split.
    /// </summary>
    public partial class uSyncDashboardApiController : UmbracoAuthorizedApiController
    {

        /// <summary>
        ///  Get the list of handler aliases to use for any give action
        /// </summary>
        [HttpPost]
        public IEnumerable<SyncHandlerView> GetActionHandlers(HandlerActions action, uSyncOptions options)
            => handlerFactory.GetValidHandlers(new SyncHandlerOptions
                {
                    Group = options.Group,
                    Action = action
                }).Select(x => new SyncHandlerView
                {
                    Enabled = x.Handler.Enabled,
                    Alias = x.Handler.Alias,
                    Name = x.Handler.Name,
                    Icon = x.Handler.Icon,
                    Group = x.Handler.Group
                });

        /// <summary>
        ///  run the report process against a given handler alias
        /// </summary>
        [HttpPost]
        public SyncActionResult ReportHandler(SyncActionOptions options) 
        {
            var hubClient = new HubClientService(options.ClientId);

            var actions = uSyncService.ReportHandler(options.Handler,
                new uSyncImportOptions
                {
                    Callbacks = hubClient.Callbacks(),
                    HandlerSet = settings.DefaultSet,
                    RootFolder = settings.RootFolder
                });

            return new SyncActionResult(actions);
        }

        /// <summary>
        ///  Run the import against a given handelr alias
        /// </summary>
        /// <remarks>
        ///  Will use the list of passed handler aliases to only import those aliase values.
        ///  this allows us to break up the import into more requests.
        /// </remarks>
        [HttpPost]
        public SyncActionResult ImportHandler(SyncActionOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);

            var actions = uSyncService.ImportHandler(options.Handler, new uSyncImportOptions
            {
                Callbacks = hubClient.Callbacks(),
                HandlerSet = settings.DefaultSet,
                RootFolder = settings.RootFolder,
                Flags = options.Force ? Core.Serialization.SerializerFlags.Force : Core.Serialization.SerializerFlags.None
            });

            return new SyncActionResult(actions);
        }

        /// <summary>
        ///  run the post import options (called after all the handlers you are going to use)
        /// </summary>
        [HttpPost]
        public SyncActionResult ImportPost(SyncActionOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);
            var actions = uSyncService.PerformPostImport(
                settings.RootFolder, settings.DefaultSet, options.Actions);

            return new SyncActionResult(actions);
        }


        /// <summary>
        ///  clean the export folder, 
        /// </summary>
        [HttpPost]
        public bool CleanExport()
        {
            try
            {
                return uSyncService.CleanExportFolder(settings.RootFolder);
            }
            catch
            {
                return false; 
            }
        }

        /// <summary>
        ///  export all the items for a given handler
        /// </summary>
        [HttpPost]
        public SyncActionResult ExportHandler(SyncActionOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);

            var actions = uSyncService.ExportHandler(options.Handler, new uSyncImportOptions
            {
                Callbacks = hubClient.Callbacks(),
                HandlerSet = settings.DefaultSet,
                RootFolder = settings.RootFolder
            });

            return new SyncActionResult(actions);
        }

        /// <summary>
        ///  trigger the start of a bulk process (fires events)
        /// </summary>
        /// <param name="action"></param>
        [HttpPost]
        public void StartProcess(HandlerActions action)
            => uSyncService.StartBulkProcess(action);

        /// <summary>
        ///  trigger the end of a bulk process (fire events)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actions"></param>
        [HttpPost]
        public void FinishProcess(HandlerActions action, IEnumerable<uSyncAction> actions)
            => uSyncService.FinishBulkProcess(action, actions);
    }
}
