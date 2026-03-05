using Umbraco.Cms.Core;

using uSync.Core.DataTypes;

namespace uSync.Community.Migrations.Codery;

internal class TextCountToTextboxConfigurationMigrator : SyncConfigurationMigratorBase, IConfigurationSerializer
{
    public string Name => nameof(TextCountToTextboxConfigurationMigrator);
    public string[] Editors => ["Codery.TextCount"];

    public override string? TargetEditor => Constants.PropertyEditors.Aliases.TextBox;

    public override IDictionary<string, object> GetMigratedConfiguration(IDictionary<string, object> configuration)
    {
        var config = new Dictionary<string, object>();

        if (configuration.TryGetValue("limit", out var limit) && int.TryParse(limit?.ToString(), out var maxChars))
        {
            config["maxChars"] = maxChars;
        }

        return config;
    }
}
