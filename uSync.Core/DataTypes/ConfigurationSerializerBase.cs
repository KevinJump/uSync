using uSync.Core.Extensions;

namespace uSync.Core.DataTypes;

public abstract class ConfigurationSerializerBase
{
    public virtual object? DeserializeConfig(string config, Type configType)
    {
        if (config.TryDeserialize(configType, out var result))
            return result;

        return default;
    }

    public virtual string? SerializeConfig(object configuration)
    {
        if (configuration.TrySerialize(out var result))
            return result;

        return default;
    }

}
