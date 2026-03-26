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

    [Obsolete("Use GetConfigurationExport(string alias, IDictionary<string, object> configuration) instead will be removed in v19")]
    IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
        => configuration;

    Task<IDictionary<string, object>> GetConfigurationExportAsync(string name, IDictionary<string, object> configuration)
        => Task.FromResult(configuration);

    [Obsolete("Use GetConfigurationExport(string alias, IDictionary<string, object> configuration) instead will be removed in v19")]
    IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => configuration;

    Task<IDictionary<string, object>> GetConfigurationImportAsync(string name, IDictionary<string, object> configuration)
        => Task.FromResult(configuration);

    string? GetEditorAlias() => null;

    string? GetEditorUIAlias() => null;

    bool IsSerializer(string propertyName)
        => Editors.InvariantContains(propertyName);
}

public interface IConfigurationTrackingSerializer : IConfigurationSerializer
{
    Task TrackRenamedEditorAsync(string oldEditorAlias, string newEditorAlias);
}