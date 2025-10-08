using System.Diagnostics.CodeAnalysis;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Serialization.Serializers;

public class SyncContentUpdateResult
{
    public SyncContentUpdateResult() { }

    [SetsRequiredMembers]
    public SyncContentUpdateResult(bool success, IContent item, string? message)
    {
        Success = success;
        Content = item;
        Message = message;
    }

    public required bool Success { get; set; }

    public required IContent Content { get; set; }

    public string? Message { get; set; }
    public Exception? Exception { get; set; }
}

public static class SyncContentUpdateResultExtensions
{
    /// <summary>
    ///  turns the PublishResult into a SyncContentUpdateResult for use in the content serializers
    /// </summary>
    public static SyncContentUpdateResult FromPublishResult(this PublishResult result)
    {
        if (result.Success)
        {
            return new SyncContentUpdateResult(true, result.Content, null);
        }

        var errorMessage = result.EventMessages?.FormatMessages(":") ?? string.Empty;
        var message = $"Publish Failed: {result.Result} [{errorMessage}]";
        if (result.InvalidProperties != null && result.InvalidProperties.Any())
        {
            message += string.Join(",", result.InvalidProperties.Select(x => x.Alias));
        }

        return new SyncContentUpdateResult(false, result.Content, message);
    }
}