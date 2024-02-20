namespace uSync.Core.DataTypes;

public interface IConfigurationSerializer
{
    string Name { get; }
    string[] Editors { get; }

    object? DeserializeConfig(string config, Type configType);

    string? SerializeConfig(object configuration);
}
