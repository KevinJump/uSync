using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Extensions;

using uSync.Core.Json;

namespace uSync.Core.DataTypes;

/// <summary>
///  migration fix, sometime in v10 -> v13, the toolbar names 
///  changed for stylesheets, so on import we now look for that
///  and fix it. 
/// </summary>
internal class RichTextEditorConfigSerializerCore : ConfigurationSerializerBase, IConfigurationSerializer
{
    private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
    {
        ContractResolver = new uSyncContractResolver()
    };

    public string Name => "RichTextEditorSerializer";

    public string[] Editors => [Constants.PropertyEditors.Aliases.TinyMce];

    public override object DeserializeConfig(string config, Type configType)
        => base.DeserializeConfig(config, configType);

    public override string SerializeConfig(object configuration)
    {
        if (!(configuration is RichTextConfiguration richTextConfiguration))
            return base.SerializeConfig(configuration);

        var editorAttempt = richTextConfiguration.Editor.TryConvertTo<JObject>();

        if (!editorAttempt.Success)
            return base.SerializeConfig(configuration);

        var toolbarAttempt = editorAttempt.Result.Value<JArray>("toolbar");

        if (toolbarAttempt.Contains("styleselect"))
        {
            toolbarAttempt.Remove("styleselect");
            toolbarAttempt.Add("styles");
        }

        editorAttempt.Result["toolbar"] = toolbarAttempt;

        return JsonConvert.SerializeObject(richTextConfiguration, Formatting.Indented, _jsonSettings);
    }
}
