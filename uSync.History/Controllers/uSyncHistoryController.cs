using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.Common.Filters;

using uSync.Backoffice.Management.Api.Configuration;
using uSync.BackOffice.Authorization;
using uSync.History.Service;

namespace uSync.History.Controllers
{
    [ApiController]
    [uSyncVersionedRoute("history")]
    [Authorize(Policy = SyncAuthorizationPolicies.TreeAccessuSync)]
    [MapToApi(SyncHistoryConstants.ApiName)]
    [DisableBrowserCache]
    [JsonOptionsName(Constants.JsonOptionsNames.BackOffice)]
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "History")]
    public class uSyncHistoryController : ControllerBase
    {
        private readonly ISyncHistoryService _syncHistoryService;

        public uSyncHistoryController(ISyncHistoryService syncHistoryService)
        {
            _syncHistoryService = syncHistoryService;
        }


        [HttpGet("IsEnabled")]
        [ProducesResponseType<bool>(200)]
        public bool HistoryIsEnabled()
            => _syncHistoryService.IsEnabled();


        [HttpGet("History")]
        [ProducesResponseType(200)]
        public async Task<IEnumerable<HistoryInfo>> GetHistory()
            => await _syncHistoryService.GetHistoryAsync();

        [HttpGet("ClearHistory")]
        [ProducesResponseType(200)]
        public void ClearHistory()
            => _syncHistoryService.ClearHistory();
    }
}
