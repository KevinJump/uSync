using uSync.Core.Extensions;

namespace uSync.Core.DataTypes;

public abstract class ConfigurationSerializerBase
{
    public virtual object? DeserializeConfig(string config, Type configType)
        => config.TryDeserialize(configType, out var value) ? value : default;

    public virtual string? SerializeConfig(object configuration)
        => configuration.TrySerializeJsonString(out var value) ? value : default;

}
