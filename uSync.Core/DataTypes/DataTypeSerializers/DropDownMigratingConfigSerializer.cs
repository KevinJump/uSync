using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

/// <summary>
///  v19 moves dropdowns with multiple:false onto their own single dropdown editor.
/// </summary>
/// <remarks>
///  Umbraco's own migration also treats a missing 'multiple' as single, but v19 no
///  longer writes the key for a multiple dropdown, so we only move it when it's set.
/// </remarks>
internal class DropDownMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    private const string _multipleKey = "multiple";

    public string Name => nameof(DropDownMigratingConfigSerializer);
    public string[] Editors => [Constants.PropertyEditors.Aliases.MultipleDropDown];

    public SyncEditorReplacement? GetEditorReplacement(string editorAlias, IDictionary<string, object> configuration)
        => configuration.TryGetBoolean(_multipleKey, out var multiple) && multiple is false
            ? new(Constants.PropertyEditors.Aliases.SingleDropDown, "Umb.PropertyEditorUi.Dropdown.Single")
            : null;

    // the editor decides single or multiple now, so the key goes either way.
    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => configuration.RemoveKeys(_multipleKey);
}
