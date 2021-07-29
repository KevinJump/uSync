using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;
using System.Web.Security;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Security;

namespace uSync.Triggers.Auth
{
    public class uSyncTriggerAuthAttribute : Attribute, IAuthenticationFilter
    {
        private static string Scheme = "Basic";
       
        public uSyncTriggerAuthAttribute() { }
            
        public bool AllowMultiple => false;

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {

            var request = context.Request;
            var authorization = request.Headers.Authorization;

            if (authorization == null) return;
            if (authorization.Scheme != "Basic") return;

            if (string.IsNullOrEmpty(authorization.Parameter))
            {
                context.ErrorResult = new AuthenticationFailureResult("Missing credentials", request);
                return;
            }

            var (username, password) = ExtractUserNameAndPassword(authorization.Parameter);

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                context.ErrorResult = new AuthenticationFailureResult("Invalid credentials", request);
                return;
            }

            var user = ValidateUmbracoUser(username, password);

            var umbracoIdentity = new UmbracoBackOfficeIdentity(
                user.Id,
                user.Username,
                user.Name,
                user.StartContentIds,
                user.StartMediaIds,
                "en-us",
                Guid.NewGuid().ToString(),
                user.SecurityStamp,
                user.AllowedSections,
                user.Groups.Select(x => x.Alias)
                );

            context.Principal = new ClaimsPrincipal(umbracoIdentity);
        }

        private (string username, string password) ExtractUserNameAndPassword(string authorizationParameter)
        {
            byte[] credentialBytes;

            try
            {
                credentialBytes = Convert.FromBase64String(authorizationParameter);
            }
            catch (FormatException)
            {
                return (null, null);
            }

            // The currently approved HTTP 1.1 specification says characters here are ISO-8859-1.
            // However, the current draft updated specification for HTTP 1.1 indicates this encoding is infrequently
            // used in practice and defines behavior only for ASCII.
            Encoding encoding = Encoding.ASCII;
            // Make a writable copy of the encoding to enable setting a decoder fallback.
            encoding = (Encoding)encoding.Clone();
            // Fail on invalid bytes rather than silently replacing and continuing.
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;
            string decodedCredentials;

            try
            {
                decodedCredentials = encoding.GetString(credentialBytes);
            }
            catch (DecoderFallbackException)
            {
                return (null, null);
            }

            if (String.IsNullOrEmpty(decodedCredentials))
            {
                return (null, null);
            }

            int colonIndex = decodedCredentials.IndexOf(':');

            if (colonIndex == -1)
            {
                return (null, null);
            }

            string userName = decodedCredentials.Substring(0, colonIndex);
            string password = decodedCredentials.Substring(colonIndex + 1);
            return (userName, password);
        }

        private IUser ValidateUmbracoUser(string username, string password)
        {
            // this is the Models builder way. 
            // but i am not sure it increments failed attempts,
            // so it would be suseptible to brute force ? 
           
            var provider = Membership.Providers[Constants.Security.UserMembershipProviderName];
            if (provider == null || !provider.ValidateUser(username, password))
                return null;
           

            var user = Current.Services.UserService.GetByUsername(username);
            if (!user.IsApproved || user.IsLockedOut)
                return null;

            return user;
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            var challenge = new AuthenticationHeaderValue(Scheme);
            context.Result = new AddChallengeOnUnauthorizedResult(challenge, context.Result);
            return Task.FromResult(0);
        }
    }
}
