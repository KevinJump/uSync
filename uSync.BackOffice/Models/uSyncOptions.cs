using System.Runtime.Serialization;

namespace uSync.BackOffice.Models;

/// <summary>
/// Options passed to Import/Export methods by JS calls
/// </summary>
public class uSyncOptions
{
    /// <summary>
    ///  SignalR Hub client id
    /// </summary>
    [DataMember(Name = "clientId")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Force the import (even if no changes detected)
    /// </summary>
    [DataMember(Name = "force")]
    public bool Force { get; set; }

    /// <summary>
    /// Make the export clean the folder before it starts 
    /// </summary>
    [DataMember(Name = "clean")]
    public bool Clean { get; set; }

    /// <summary>
    /// Name of the handler group to perform the actions on
    /// </summary>
    [DataMember(Name = "group")]
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// The set in which the handler lives
    /// </summary>
    [DataMember(Name = "set")]
    public string Set { get; set; } = string.Empty;
}
