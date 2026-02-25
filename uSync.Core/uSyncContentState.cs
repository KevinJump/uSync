using System.Text.Json.Serialization;

namespace uSync.Core;

/// <summary>
///  the state we thing a culture / content item should be in.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<uSyncContentState>))]
public enum uSyncContentState
{
    Saved, Unpublished, Published
}
