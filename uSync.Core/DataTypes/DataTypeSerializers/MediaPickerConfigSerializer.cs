using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;
internal class MediaPickerConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    public string Name => nameof(MediaPickerConfigSerializer);

    public string[] Editors => [
        "Umbraco.MediaPicker",
        "Umbraco.MediaPicker2"];

    /// <summary>
    ///  we migrate this one, from "Umbraco.MediaPicker" to "Umbraco.MediaPicker3" 
    /// </summary>
    public string? GetEditorAlias()
        => Constants.PropertyEditors.Aliases.MediaPicker3;

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        return base.GetConfigurationImport(configuration);
    }
}
