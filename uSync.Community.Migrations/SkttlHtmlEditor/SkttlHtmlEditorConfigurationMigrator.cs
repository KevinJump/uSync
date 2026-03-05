using Umbraco.Cms.Core.PropertyEditors;

using uSync.Core.DataTypes;
using uSync.Core.Mapping;

namespace uSync.Community.Migrations.Contentment;

[RequiresPropertyEditor("Umbraco.Community.Contentment.CodeEditor")]
public class SkttlHtmlEditorConfigurationMigrator : SyncDependenantConfigurationMigratorBase, IConfigurationSerializer
{
    public string Name => nameof(SkttlHtmlEditorConfigurationMigrator);
    public override string[] Editors => ["skttl.HtmlEditor"];
    public override string? TargetEditor => "Umbraco.Community.Contentment.CodeEditor";

    public SkttlHtmlEditorConfigurationMigrator(PropertyEditorCollection propertyEditors)
        : base(propertyEditors)
    { }

    public override IDictionary<string, object> GetMigratedConfiguration(IDictionary<string, object> configuration)
    {
        return new Dictionary<string, object>
        {
            { "mode", "razor" },
            { "theme", "chrome" },
            { "fontSize", "small" },
            { "useWrapMode", 0 },
            { "minLines", 12 },
            { "maxLines", 30 }
        };
    }
}

