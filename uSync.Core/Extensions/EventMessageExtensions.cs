using Umbraco.Cms.Core.Events;

namespace uSync.Core;

public static class EventMessageExtensions
{
    public static string FormatMessages(this EventMessages eventMessages, string seprerator = " : ")
    {
        if (eventMessages != null)
        {
            return string.Join(seprerator,
                eventMessages.GetAll()
                    .Select(x => $"{x.Category} {x.Message}"));
        }

        return string.Empty;
    }
}
