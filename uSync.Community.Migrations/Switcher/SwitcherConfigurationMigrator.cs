using Umbraco.Cms.Core;

using uSync.Core.DataTypes;

namespace uSync.Community.Migrations.Switcher;

public class SwitcherConfigurationMigrator : SyncConfigurationMigratorBase, IConfigurationSerializer
{
    public string Name => nameof(SwitcherConfigurationMigrator);

    public string[] Editors => ["Our.Umbraco.Switcher"];
    public override string? TargetEditor => Constants.PropertyEditors.Aliases.Boolean;

    public override IDictionary<string, object> GetMigratedConfiguration(IDictionary<string, object> configuration)
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
