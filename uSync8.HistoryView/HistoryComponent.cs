using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using Newtonsoft.Json;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.JavaScript;

using uSync8.BackOffice;
using uSync8.BackOffice.Configuration;
using uSync8.BackOffice.Services;
using uSync8.Core;
using uSync8.HistoryView.Controllers;

namespace uSync8.HistoryView
{
    [ComposeAfter(typeof(uSyncBackOfficeComposer))]
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class HistoryComposer : ComponentComposer<HistoryComponent>
    { }

    public class HistoryComponent : IComponent
    {
        private readonly IUmbracoContextFactory umbracoContextFactory;

        private readonly SyncFileService syncFileService;
        private readonly string historyFolder;
        private readonly uSyncSettings uSyncSettings;
        private readonly IProfilingLogger logger;


        public HistoryComponent(SyncFileService syncFileService,
            uSyncConfig syncConfig,
            IProfilingLogger logger,
            IUmbracoContextFactory umbracoContextFactory,
            IGlobalSettings globalSettings)
        {
            this.logger = logger;

            this.umbracoContextFactory = umbracoContextFactory;
            this.uSyncSettings = syncConfig.Settings;

            this.syncFileService = syncFileService;
            historyFolder = Path.Combine(globalSettings.LocalTempPath, "usync", "history");

            uSyncService.ImportComplete += USyncService_ImportComplete;
        }


        public void Initialize()
        {
            ServerVariablesParser.Parsing += ServerVariablesParser_Parsing;
        }

        private void ServerVariablesParser_Parsing(object sender, Dictionary<string, object> e)
        {
            if (HttpContext.Current == null)
                throw new InvalidOperationException("This method requires that an HttpContext be active");

            var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

            var uSync = e["uSync"];
            if (uSync != null && uSync is Dictionary<string, object> uSyncSettings)
            {
                uSyncSettings.Add("historyService", urlHelper.GetUmbracoApiServiceBaseUrl<uSyncHistoryApiController>(controller => controller.GetApi()));
            }
        }

        private void USyncService_ImportComplete(uSyncBulkEventArgs e)
        {
            if (!uSyncSettings.EnableHistory) return;

            using (logger.DebugDuration<HistoryComponent>("History - ImportComplete", "History - ImportComplete Finished"))
            {
                var changes = e.Actions.Where(x => x.Change > ChangeType.NoChange);

                if (changes.Any())
                {
                    var changeDetail = new SyncHistoryView
                    {
                        Server = HttpContext.Current?.Server?.MachineName ?? "Unknown",
                        Action = "Import",
                        Username = GetUsername(),
                        When = DateTime.Now,
                        Changes = changes.ToList()
                    };

                    var actionString = JsonConvert.SerializeObject(changeDetail, Formatting.Indented);
                    var filename = $"History_{DateTime.Now:yyyyMMdd_HHmmss}.history";
                    var path = Path.Combine(historyFolder, DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"), filename);
                    syncFileService.SaveFile(path, actionString);
                }
            }
        }

        private string GetUsername()
        {
            using (var contextReference = umbracoContextFactory.EnsureUmbracoContext())
            {
                var username = contextReference?.UmbracoContext?.Security?.CurrentUser?.Username;

                if (string.IsNullOrWhiteSpace(username))
                {
                    username = "Background Process";
                }

                return username;
            }
        }

        public void Terminate()
        {
            // nothing...
        }
    }
}
