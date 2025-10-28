using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using uSync.BackOffice;
using uSync.Backoffice.Management.Api.Models;

namespace uSync.History
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]

    public class HistoryInfo
    {
        public IEnumerable<uSyncActionView> Actions { get; set; } = [];

        public DateTime Date { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public int Changes { get; set; }

        public int Total { get; set; }
    }
}
