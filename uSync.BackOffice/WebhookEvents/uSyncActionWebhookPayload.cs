using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using uSync.Core;
using uSync.Core.Models;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  A serializable version of a uSyncAction, sent in webhook payloads
/// </summary>
public class uSyncActionWebhookPayload
{
    /// <summary>
    ///  Alias of the handler 
    /// </summary>
    public string? HandlerAlias { get; set; }

    /// <summary>
    ///  Was the action a success
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///  Type name of the item the action was performed on
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    ///  message to display along with action
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    ///  message of the exception encountered during the action
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    ///  type of change performed 
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChangeType Change { get; set; }

    /// <summary>
    ///  path name for the uSync file
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    ///  display name of the item
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///  a nice path for the thing (displayed).
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///  this action still requires some processing 
    /// </summary>
    public bool RequiresPostProcessing { get; set; }

    /// <summary>
    ///  text that is shown on the details screen above any details. 
    /// </summary>
    public string? DetailMessage { get; set; }

    /// <summary>
    ///  list of detailed changes, so you can see what is changing.
    /// </summary>
    public IEnumerable<uSyncChange>? Details { get; set; }

    /// <summary>
    ///  the GUID key value of the item 
    /// </summary>
    public Guid Key { get; set; }

    /// <summary>
    ///  build a payload from a uSyncAction
    /// </summary>
    public static uSyncActionWebhookPayload FromAction(uSyncAction action)
        => new()
        {
            HandlerAlias = action.HandlerAlias,
            Success = action.Success,
            ItemType = action.ItemType,
            Message = action.Message,
            ExceptionMessage = action.Exception?.Message,
            Change = action.Change,
            FileName = action.FileName,
            Name = action.Name,
            Path = action.Path,
            RequiresPostProcessing = action.RequiresPostProcessing,
            DetailMessage = action.DetailMessage,
            Details = action.Details?.ToList(),
            Key = action.Key
        };
}
