using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Web.BackOffice.Controllers;

using uSync.BackOffice.Hubs;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice.Assets.Controllers
{
    /// <summary>
    ///  Actions by handler, allows for things to be split.
    /// </summary>
    public partial class uSyncDashboardApiController : UmbracoAuthorizedJsonController
    {
        private readonly ISyncActionService _syncActionService;

        /// <summary>
        ///  Get the list of handler aliases to use for any give action
        /// </summary>
        [HttpPost]
        public IEnumerable<SyncHandlerView> GetActionHandlers([FromQuery]HandlerActions action, uSyncOptions options)
            => _syncActionService.GetActionHandlers(action, options);

        /// <summary>
        ///  run the report process against a given handler alias
        /// </summary>
        [HttpPost]
        public SyncActionResult ReportHandler(SyncActionOptions options)
        {
            var hubClient = new HubClientService(_hubContext, options.ClientId);
            return _syncActionService.ReportHandler(options, hubClient.Callbacks());
        }

        /// <summary>
        ///  Run the import against a given handler alias
        /// </summary>
        /// <remarks>
        ///  Will use the list of passed handler aliases to only import those alias values.
        ///  this allows us to break up the import into more requests.
        /// </remarks>
        [HttpPost]
        public SyncActionResult ImportHandler(SyncActionOptions options)
        {
            var hubClient = new HubClientService(_hubContext, options.ClientId);
            return _syncActionService.ImportHandler(options, hubClient.Callbacks());
        }

        /// <summary>
        ///  run the post import options (called after all the handlers you are going to use)
        /// </summary>
        [HttpPost]
        public SyncActionResult ImportPost(SyncActionOptions options)
        {
            var hubClient = new HubClientService(_hubContext, options.ClientId);
            return _syncActionService.ImportPost(options, hubClient.Callbacks());
        }


        /// <summary>
        ///  clean the export folder, 
        /// </summary>
        [HttpPost]
        public bool CleanExport()
        {
            _syncActionService.CleanExportFolder();
            return true;
        }

        /// <summary>
        ///  export all the items for a given handler
        /// </summary>
        [HttpPost]
        public SyncActionResult ExportHandler(SyncActionOptions options)
        {
            var hubClient = new HubClientService(_hubContext, options.ClientId);
            return _syncActionService.ExportHandler(options, hubClient.Callbacks());
        }

        /// <summary>
        ///  trigger the start of a bulk process (fires events)
        /// </summary>
        /// <param name="action"></param>
        [HttpPost]
        public void StartProcess([FromQuery]HandlerActions action)
            => _syncActionService.StartProcess(action);

        /// <summary>
        ///  trigger the end of a bulk process (fire events)
        /// </summary>
        /// <param name="action"></param>
        /// <param name="actions"></param>
        [HttpPost]
        public void FinishProcess([FromQuery] HandlerActions action, IEnumerable<uSyncAction> actions)
            => _syncActionService.FinishProcess(action, actions, GetCurrentUser());

        private string GetCurrentUser()
        {
            try
            {
                return _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Name ?? "User";
            }
            catch
            {
                return "User";
            }
        }
    }
}
