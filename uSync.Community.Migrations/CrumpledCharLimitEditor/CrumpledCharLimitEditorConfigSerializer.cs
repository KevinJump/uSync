using uSync.Core.DataTypes;

namespace uSync.Community.Migrations.CrumpledCharLimitEditor;

internal class CrumpledCharLimitEditorConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    public string Name => nameof(CrumpledCharLimitEditorConfigSerializer);
    public string[] Editors => ["Crumpled.CharLimitEditor"];

    public string? GetEditorAlias() => "UmbConstants.PropertyEditors.Aliases.TextBox";

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        return MigratePropertyNames(configuration, new Dictionary<string, string>
        {
            { "limit", "maxChars"},
        });
    }

}
