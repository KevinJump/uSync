using Umbraco.Cms.Core.PropertyEditors;

using uSync.Core.DataTypes;
using uSync.Core.Mapping;

namespace uSync.Community.Migrations.Contentment;

[RequiresPropertyEditor("Umbraco.Community.Contentment.CodeEditor")]
public class SkttlHtmlEditorConfigSerializer : ConfigurationDependenantSerializerBase, IConfigurationSerializer
{
    public string Name => nameof(SkttlHtmlEditorConfigSerializer);
    public override string[] Editors => ["skttl.HtmlEditor"];

    public SkttlHtmlEditorConfigSerializer(PropertyEditorCollection propertyEditors)
        : base(propertyEditors)
    { }

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
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

