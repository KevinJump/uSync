using System.Diagnostics.CodeAnalysis;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Serialization.Serializers;

public class SyncContentUpdateResult<TObject>
    where TObject : class, IPublishableContentBase
{
    public SyncContentUpdateResult() { }

    [SetsRequiredMembers]
    public SyncContentUpdateResult(bool success, TObject? item, string? message)
    {
        Success = success;
        Content = item;
        Message = message;
    }

    public required bool Success { get; set; }

    public required TObject? Content { get; set; }

    public string? Message { get; set; }
    public Exception? Exception { get; set; }
}

public static class SyncContentUpdateResultExtensions
{
    /// <summary>
    ///  turns the PublishResult into a SyncContentUpdateResult for use in the content serializers
    /// </summary>
    public static SyncContentUpdateResult<TObject> FromPublishResult<TObject>(this PublishResult result)
        where TObject : class, IPublishableContentBase
    {
        if (result.Success && result.Content is TObject content)
        {
            return new SyncContentUpdateResult<TObject>(true, content, null);
        }

        var errorMessage = result.EventMessages?.FormatMessages(":") ?? string.Empty;
        var message = $"Publish Failed: {result.Result} [{errorMessage}]";
        if (result.InvalidProperties != null && result.InvalidProperties.Any())
        {
            message += string.Join(",", result.InvalidProperties.Select(x => x.Alias));
        }

        return new SyncContentUpdateResult<TObject>(false, result.Content as TObject ?? null, message);
    }
}