using System;
using System.Xml.Linq;

using uSync.Core;

namespace uSync.BackOffice.WebhookEvents;

/// <summary>
///  Payload sent by the item imported/exported webhook events
/// </summary>
public class uSyncItemWebhookPayload
{
    /// <summary>
    ///  Key of the item (null when not known)
    /// </summary>
    public Guid? Key { get; set; }

    /// <summary>
    ///  Alias of the item (null when not known)
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    ///  Type of the item (null when not known)
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    ///  The type of change (e.g Import, Export)
    /// </summary>
    public string Change { get; set; } = string.Empty;

    /// <summary>
    ///  build a payload from the item xml and the change type
    /// </summary>
    public static uSyncItemWebhookPayload FromItem(XElement? item, ChangeType change)
    {
        var key = item?.GetKey() ?? Guid.Empty;
        var alias = item?.GetAlias();
        var itemType = item?.GetItemType();

        return new uSyncItemWebhookPayload
        {
            Key = key == Guid.Empty ? null : key,
            Alias = string.IsNullOrEmpty(alias) ? null : alias,
            ItemType = string.IsNullOrEmpty(itemType) ? null : itemType,
            Change = change.ToString()
        };
    }
}
