using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
                    Alias = x.Handler.Alias,
                    Name = x.Handler.Name,
                    Icon = x.Handler.Icon,
                    Group = x.Handler.Group
                });

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
        ///  Import
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

        [HttpPost]
        public SyncActionResult ImportPost(SyncActionOptions options)
        {
            var hubClient = new HubClientService(options.ClientId);
            var actions = uSyncService.PerformPostImport(
                settings.RootFolder, settings.DefaultSet, options.Actions);

            return new SyncActionResult(actions);
        }

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

        [HttpPost]
        public void StartProcess(HandlerActions action)
            => uSyncService.StartBulkProcess(action);

        [HttpPost]
        public void FinishProcess(HandlerActions action, IEnumerable<uSyncAction> actions)
            => uSyncService.FinishBulkProcess(action, actions);
    }

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncActionOptions
    {
        public string ClientId { get; set; }
        public string Handler { get; set; }
        public bool Force { get; set; }

        public IEnumerable<uSyncAction> Actions { get; set; }

    }

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncActionResult {
        
        public SyncActionResult() { }
        public SyncActionResult(IEnumerable<uSyncAction> actions)
        {
            this.Actions = actions;
        }

        public IEnumerable<uSyncAction> Actions { get; set; }
    }
}
