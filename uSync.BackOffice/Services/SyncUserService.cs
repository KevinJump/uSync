using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

using uSync.BackOffice.Configuration;

namespace uSync.BackOffice.Services;

/// <inheritdoc cref="ISyncUserService"/>
internal class SyncUserService : ISyncUserService
{
    private readonly ILogger<SyncUserService> _logger;
    private readonly ISyncConfigService _syncConfigService;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IUserService _userService;

    public SyncUserService(
        ILogger<SyncUserService> logger,
        ISyncConfigService syncConfigService,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IUserService userService)
    {
        _logger = logger;
        _syncConfigService = syncConfigService;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _userService = userService;
    }

    /// <inheritdoc/>
    public async Task<int> GetImportUserIdAsync(int requestedUserId)
    {
        if (requestedUserId != Constants.Security.UnknownUserId)
            return requestedUserId;

        var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (currentUser is not null)
            return currentUser.Id;

        var backgroundUserKey = _syncConfigService.Settings.BackgroundUser;
        if (backgroundUserKey is null)
            return Constants.Security.SuperUserId;

        var backgroundUser = await _userService.GetAsync(backgroundUserKey.Value);
        if (backgroundUser is not null)
            return backgroundUser.Id;

        _logger.LogWarning("uSync: BackgroundUser {key} doesn't exist, running the import as the superuser", backgroundUserKey);
        return Constants.Security.SuperUserId;
    }
}
