
using Umbraco.Core;
using Umbraco.Core.Services;

namespace uSync8.ContentEdition
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
            return Attempt.Fail($"Publish failed: {result.Result} {errorMessage}");
        }
    }
}