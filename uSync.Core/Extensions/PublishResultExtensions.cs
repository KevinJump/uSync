
using System.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace uSync.Core
{
    public static class PublishResultExtensions
    {
        /// <summary>
        ///  turns a PublishResult into an Attempt
        /// </summary>
        public static Attempt<string> ToAttempt(this PublishResult result)
        {
            if (result.Success) return Attempt.Succeed("Published");

            var errorMessage = result.EventMessages.FormatMessages(":");
            var message = $"Publish Failed: {result.Result} [{errorMessage}]";

            if (result.InvalidProperties != null && result.InvalidProperties.Any())
            {
                message += string.Join(",", result.InvalidProperties.Select(x => x.Alias));
            }

            return Attempt.Fail(message);
        }
    }
}