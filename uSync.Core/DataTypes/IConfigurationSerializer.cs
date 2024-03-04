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

    IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
        => configuration;

    IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => configuration;
}
