using uSync.Core.DataTypes;

namespace uSync.Community.Migrations.Switcher;

public class SwitcherConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    public string Name => nameof(SwitcherConfigSerializer);

    public string[] Editors => ["Our.Umbraco.Switcher"];

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        return MigratePropertyNames(configuration, new Dictionary<string, string>
        {
            { "hideLabel", "showLabels" },
            { "onLabelText", "onLabel" },
            { "offLabelText", "offLabel" },
            { "switchOn", "default" }
        });
    }    
}
