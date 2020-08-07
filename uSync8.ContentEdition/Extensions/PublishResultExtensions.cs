using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var errorMessage = "";
            if (result.EventMessages.Count > 0)
            {
                errorMessage = string.Join(": ", result.EventMessages.GetAll().Select(x => $"{x.Category}: {x.Message}"));
            }

            return Attempt.Fail($"Publish Failed {result.Result} {errorMessage}");
        }
    }
}