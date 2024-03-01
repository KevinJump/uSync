using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;

using uSync.Core.Extensions;

namespace uSync.Core.DataTypes;

/// <summary>
///  migration fix, sometime in v10 -> v13, the toolbar names 
///  changed for stylesheets, so on import we now look for that
///  and fix it. 
/// </summary>
internal class RichTextEditorConfigSerializerCore : ConfigurationSerializerBase, IConfigurationSerializer
{
    public string Name => "RichTextEditorSerializer";

    public string[] Editors => [Constants.PropertyEditors.Aliases.TinyMce];

    public override object? DeserializeConfig(string config, Type configType)
        => base.DeserializeConfig(config, configType);

    public override string? SerializeConfig(object configuration)
    {
        if (!(configuration is RichTextConfiguration richTextConfiguration))
            return base.SerializeConfig(configuration);

        //if (richTextConfiguration?.Editor == null)
        //    return base.SerializeConfig(configuration);

        //if (richTextConfiguration.Editor.TryConvertToJsonObject(out var jsonObject) is false || jsonObject is null)
        //    return base.SerializeConfig(configuration);

        //var toolbar = jsonObject.GetPropertyAsArray("toolbar");
        //if (toolbar.Contains("styleselect") is true)
        //{
        //    toolbar.Remove("styleselect");
        //    toolbar.Add("styles");
        //}

        //jsonObject["toolbar"] = toolbar;

        //richTextConfiguration.Editor = jsonObject.TryConvertTo<object>();

        return richTextConfiguration.SerializeJsonString();
    }
}
