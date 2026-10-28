using Umbraco.Cms.Core;
using Umbraco.Cms.Core.PropertyEditors;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class LabelMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    private const string _valueTypeKey = Constants.PropertyEditors.ConfigurationKeys.DataValueType;

    public string Name => nameof(LabelMigratingConfigSerializer);
    public string[] Editors => [Constants.PropertyEditors.Aliases.Label];

    /// <summary>
    ///  v19 gives each label value type its own editor (Umbraco.Label.Integer, etc.)
    /// </summary>
    public SyncEditorReplacement? GetEditorReplacement(string editorAlias, IDictionary<string, object> configuration)
    {
        if (configuration.TryGetString(_valueTypeKey, out var valueType) is false
            && configuration.TryGetString("valueType", out valueType) is false)
            return null;

        return valueType.ToUpperInvariant() switch
        {
            ValueTypes.Integer => new(Constants.PropertyEditors.Aliases.LabelInteger, "Umb.PropertyEditorUi.Label.Integer"),
            ValueTypes.Bigint => new(Constants.PropertyEditors.Aliases.LabelBigInt, "Umb.PropertyEditorUi.Label.BigInt"),
            ValueTypes.Decimal => new(Constants.PropertyEditors.Aliases.LabelDecimal, "Umb.PropertyEditorUi.Label.Decimal"),
            ValueTypes.DateTime or ValueTypes.Date => new(Constants.PropertyEditors.Aliases.LabelDateTime, "Umb.PropertyEditorUi.Label.DateTime"),
            ValueTypes.Time => new(Constants.PropertyEditors.Aliases.LabelTime, "Umb.PropertyEditorUi.Label.Time"),
            ValueTypes.Text or ValueTypes.Json or ValueTypes.Xml => new(Constants.PropertyEditors.Aliases.LabelText, "Umb.PropertyEditorUi.Label.Text"),
            _ => null,
        };
    }

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => MigratePropertyNames(configuration, new()
        {
            { "ValueType", _valueTypeKey }
        })
        // v19: the value type is now the editor, so it is no longer config.
        .RemoveKeys(_valueTypeKey);
}
