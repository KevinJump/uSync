using Umbraco.Extensions;

namespace uSync.Core.DataTypes;

public interface IConfigurationSerializer
{
    /// <summary>
    ///  Name of the serializer
    /// </summary>
    string Name { get; }

    /// <summary>
    ///  array of editor aliases that this serializer works for.
    /// </summary>
    string[] Editors { get; }

    Task<IDictionary<string, object>> GetConfigurationExportAsync(string name, IDictionary<string, object> configuration)
        => Task.FromResult(configuration);

    Task<IDictionary<string, object>> GetConfigurationImportAsync(string name, IDictionary<string, object> configuration)
        => Task.FromResult(configuration);

    string? GetEditorAlias() => null;

    string? GetEditorUIAlias() => null;

    /// <summary>
    ///  the editor this configuration should be imported onto, when it isn't the one in the file.
    /// </summary>
    /// <remarks>
    ///  for editors Umbraco has split by configuration (e.g. v19 moved a media picker
    ///  with multiple:false onto its own single media picker editor), the editor
    ///  depends on the config being imported, so GetEditorAlias() can't express it.
    /// </remarks>
    SyncEditorReplacement? GetEditorReplacement(string editorAlias, IDictionary<string, object> configuration)
        => null;

    bool IsSerializer(string propertyName)
        => Editors.InvariantContains(propertyName);
}

public interface IConfigurationTrackingSerializer : IConfigurationSerializer
{
    Task TrackRenamedEditorAsync(string oldEditorAlias, string newEditorAlias);
}

/// <summary>
///  the editor (and editor UI) a data type should be moved onto during an import.
/// </summary>
public record SyncEditorReplacement(string EditorAlias, string EditorUiAlias);