namespace uSync.Core.DataTypes;

public abstract class ConfigurationSerializerBase
{
    public virtual IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
          => configuration;

    public virtual IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => configuration;
}
