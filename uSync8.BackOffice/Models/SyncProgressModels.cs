using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncProgressSummary
    {
        public int Count { get; set; }
        public int Total { get; set; }
        public string Message { get; set; }
        public List<SyncHandlerSummary> Handlers { get; set; }

        public SyncProgressSummary(
            IEnumerable<ISyncHandler> handlers, 
            string message,
            int totalSteps)
        {
            this.Total = totalSteps;
            this.Message = message;

            this.Handlers = handlers.Select(x => new SyncHandlerSummary()
            {
                Icon = x.Icon,
                Name = x.Name,
                Status = HandlerStatus.Pending
            }).ToList();
        }       

        public void UpdateHandler(string name, HandlerStatus status, int changeCount)
        {
            var item = this.Handlers.FirstOrDefault(x => x.Name == name);
            if (item != null)
            {
                item.Status = status;
                item.Changes = changeCount;
            }

        }

        public void UpdateHandler(string name, HandlerStatus status, string message, int changeCount)
        {
            UpdateHandler(name, status, changeCount);
            this.Message = message;
        }

    }

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncHandlerSummary
    {
        public string Icon { get; set; }
        public string Name { get; set; }
        public HandlerStatus Status { get; set; }

        public int Changes { get; set; }
    }

    public enum HandlerStatus
    {
        Pending,
        Processing,
        Complete,
        Error
    }
}
