using System.Linq;

using Umbraco.Core.Events;

namespace uSync8.ContentEdition
{
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
}
