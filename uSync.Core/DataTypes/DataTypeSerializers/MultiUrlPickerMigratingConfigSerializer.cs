using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

/// <summary>
///  v19 moves multi url pickers with maxNumber:1 onto the single url picker,
///  and min/max on the rest move to a single validation limit.
/// </summary>
internal class MultiUrlPickerMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    private const string _minNumberKey = "minNumber";
    private const string _maxNumberKey = "maxNumber";

    public string Name => nameof(MultiUrlPickerMigratingConfigSerializer);
    public string[] Editors => [Constants.PropertyEditors.Aliases.MultiUrlPicker];

    public SyncEditorReplacement? GetEditorReplacement(string editorAlias, IDictionary<string, object> configuration)
        => IsSingleLink(configuration)
            ? new(Constants.PropertyEditors.Aliases.SingleUrlPicker, "Umb.PropertyEditorUi.UrlPicker.Single")
            : null;

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => IsSingleLink(configuration)
            ? configuration.RemoveKeys(_minNumberKey, _maxNumberKey)
            : configuration.MigrateMinMaxToRange(_minNumberKey, _maxNumberKey, "validationLimit");

    private static bool IsSingleLink(IDictionary<string, object> configuration)
        => configuration.TryGetDecimal(_maxNumberKey, out var maxNumber) && maxNumber == 1;
}
