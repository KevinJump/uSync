using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace uSync.BackOffice.Hubs;

public class uSyncHubMiddleware : IMiddleware
{
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

    public uSyncHubMiddleware(IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
    {
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        CookieAuthenticationOptions cookieOptions = context.RequestServices
                    .GetRequiredService<IOptionsSnapshot<CookieAuthenticationOptions>>()
                    .Get(Constants.Security.BackOfficeAuthenticationType);

        if (cookieOptions.Cookie.Name is null)
        {
            await next(context);
            return;
        }
        var chunkingCookieManager = new ChunkingCookieManager();
        var cookie = chunkingCookieManager.GetRequestCookie(context, cookieOptions.Cookie.Name);

        if (!string.IsNullOrEmpty(cookie))
        {
            AuthenticationTicket unprotected = cookieOptions.TicketDataFormat.Unprotect(cookie);
            ClaimsIdentity backOfficeIdentity = unprotected?.Principal.GetUmbracoIdentity();

            if (backOfficeIdentity != null)
            {
                // Ok, we've got a real ticket, now we can add this ticket's identity to the current
                // Principal, this means we'll have 2 identities assigned to the principal which we can
                // use to authorize the preview and allow for a back office User.
                context.User = new ClaimsPrincipal(backOfficeIdentity);
                EnsureBackOfficeUser();
            }
        }

        await next(context);
    }

    private void EnsureBackOfficeUser()
        => _ = _backOfficeSecurityAccessor.BackOfficeSecurity.CurrentUser;
}
