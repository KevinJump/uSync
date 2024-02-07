using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using uSync.BackOffice.SyncHandlers;

namespace uSync.BackOffice
{
    /// <summary>
    ///  Progress summary - object that tells the UI to draw the handler icons while uSync works.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
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

        private SyncProgressSummary(string message, int totalSteps)
        {
            this.Message = message;
            this.Total = totalSteps;
        }

        /// <summary>
        ///  Create a new SyncProcessSummary object 
        /// </summary>
        public SyncProgressSummary(IEnumerable<SyncHandlerSummary> summaries,
            string message, int totalSteps)
            : this(message, totalSteps)
        {
            this.Handlers = summaries.ToList();
        }

        /// <summary>
        ///  Create a new SyncProcessSummary object 
        /// </summary>
        public SyncProgressSummary(
            IEnumerable<ISyncHandler> handlers,
            string message,
            int totalSteps)
            : this(message, totalSteps)
        {
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
            UpdateMessage(message);
        }

        /// <summary>
        ///  update the progress message in the object
        /// </summary>
        /// <param name="message"></param>
        public void UpdateMessage(string message)
        {
            this.Message = message;
        }

        /// <summary>
        ///  increment the item count in the progress message 
        /// </summary>
        public void Increment()
        {
            this.Count++;
        }

    }

    /// <summary>
    ///  Summary object used to display a summary of progress via the UI
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
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
        /// <summary>
        ///  Pending - handler has yet to start work 
        /// </summary>
        Pending,

        /// <summary>
        ///  Processing - handler is doing stuff now 
        /// </summary>
        Processing,

        /// <summary>
        ///  Complete - handler has completed its work 
        /// </summary>
        Complete,

        /// <summary>
        ///  error - the handler encountered one or more errors
        /// </summary>
        Error
    }
}
