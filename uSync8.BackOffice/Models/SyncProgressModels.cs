using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using uSync8.BackOffice.SyncHandlers;

namespace uSync8.BackOffice
{
    /// <summary>
    ///  Progress summary - object that tells the UI to draw the handler icons while uSync works.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncProgressSummary
    {
        /// <summary>
        ///  current count (progress) of where we are upto.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        ///  How many steps we think we are going to take.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        ///  Message to display to user.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///  Summary (icons, state) of the handlers 
        /// </summary>
        public List<SyncHandlerSummary> Handlers { get; set; }

        public SyncProgressSummary(
            IEnumerable<ISyncHandler> handlers,
            string message,
            int totalSteps)
        {
            this.Total = totalSteps;
            this.Message = message;

            if (handlers != null)
            {
                this.Handlers = handlers.Select(x => new SyncHandlerSummary()
                {
                    Icon = x.Icon,
                    Name = x.Name,
                    Status = HandlerStatus.Pending
                }).ToList();
            }
            else
            {
                this.Handlers = new List<SyncHandlerSummary>();
            }
        }

        public SyncProgressSummary(IEnumerable<SyncHandlerSummary> summaries,
            string message, int totalSteps)
        {
            this.Total = totalSteps;
            this.Message = message;
            this.Handlers = summaries.ToList();
        }

        /// <summary>
        ///  Updated the change status of a single handler in the list
        /// </summary>
        /// <param name="name">Name of handler</param>
        /// <param name="status">current handler status</param>
        /// <param name="changeCount">number of changes</param>
        public void UpdateHandler(string name, HandlerStatus status, int changeCount)
        {
            UpdateHandler(name, status, changeCount, false);
        }

        /// <summary>
        ///  Updated the change status of a single handler in the list
        /// </summary>
        /// <param name="name">Name of handler</param>
        /// <param name="status">current handler status</param>
        /// <param name="changeCount">number of changes</param>
        /// <param name="hasErrors">there are actions that have failed in the set</param>
        public void UpdateHandler(string name, HandlerStatus status, int changeCount, bool hasErrors)
        {
            var item = this.Handlers.FirstOrDefault(x => x.Name == name);
            if (item != null)
            {
                item.Status = status;
                item.Changes = changeCount;
                item.InError = hasErrors;
            }
        }

        /// <summary>
        ///  Updated the change status of a single handler in the list
        /// </summary>
        /// <param name="name">Name of handler</param>
        /// <param name="status">current handler status</param>
        /// <param name="message">Update the main progress message for the UI</param>
        /// <param name="changeCount">number of changes</param>
        public void UpdateHandler(string name, HandlerStatus status, string message, int changeCount)
        {
            UpdateHandler(name, status, changeCount);
            this.Message = message;
        }

    }

    [JsonObject(NamingStrategyType = typeof(DefaultNamingStrategy))]
    public class SyncHandlerSummary
    {
        /// <summary>
        ///  The icon the user sees for this handler.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        ///  Name shown under the handler
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  Current status of the handler
        /// </summary>
        public HandlerStatus Status { get; set; }

        /// <summary>
        ///  number of changes that have been processed
        /// </summary>
        public int Changes { get; set; }

        /// <summary>
        ///  reports if the handler has errors
        /// </summary>
        public bool InError { get; set; }
    }

    /// <summary>
    ///  current status of a handler.
    /// </summary>
    public enum HandlerStatus
    {
        Pending,
        Processing,
        Complete,
        Error
    }
}
