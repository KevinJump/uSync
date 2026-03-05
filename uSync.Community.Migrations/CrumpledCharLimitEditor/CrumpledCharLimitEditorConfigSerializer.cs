using Umbraco.Cms.Core;

using uSync.Core.DataTypes;

namespace uSync.Community.Migrations.CrumpledCharLimitEditor;

internal class CrumpledCharLimitEditorConfigurationMigrator : SyncConfigurationMigratorBase, IConfigurationSerializer
{
    public string Name => nameof(CrumpledCharLimitEditorConfigurationMigrator);
    public string[] Editors => ["Crumpled.CharLimitEditor"];
    public override string? TargetEditor => Constants.PropertyEditors.Aliases.TextBox;

    public override IDictionary<string, object> GetMigratedConfiguration(IDictionary<string, object> configuration)
    {
        return MigratePropertyNames(configuration, new Dictionary<string, string>
        {
            { "limit", "maxChars"},
        });
    }
}
