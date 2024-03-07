using Umbraco.Cms.Core.Events;

namespace uSync.Core;

public static class EventMessageExtensions
{
    /// <summary>
    ///  formats a list of EventMessages (from a publish or save) into something we can log/display
    /// </summary>
    public static string FormatMessages(this EventMessages eventMessages, string separator = " : ")
        => eventMessages is not null
            ? string.Join(separator, eventMessages.GetAll().Select(x => $"{x.Category} {x.Message}"))
            : string.Empty;
}
