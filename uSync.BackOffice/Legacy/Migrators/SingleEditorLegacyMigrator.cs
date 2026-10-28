using System;
using System.Linq;
using System.Xml.Linq;

namespace uSync.BackOffice.Legacy.Migrators;

/// <summary>
///  moves pickers that don't say if they allow multiple items onto the v19 single editor.
/// </summary>
/// <remarks>
///  Umbraco's v19 upgrade treats a missing 'multiple' as single. A standard import can't,
///  because v19's own multiple editors also save no 'multiple' key. In the legacy flow we
///  know the files come from an older version, so we can.
/// </remarks>
internal abstract class SingleEditorLegacyMigrator : ISyncLegacyMigrator
{
    private const string _multipleKey = "multiple";
    private const int _firstVersionWithSplit = 19;

    private readonly string _sourceAlias;
    private readonly string _targetAlias;
    private readonly string _targetUiAlias;
    private readonly string[] _removeKeys;

    protected SingleEditorLegacyMigrator(string sourceAlias, string targetAlias, string targetUiAlias, params string[] removeKeys)
    {
        _sourceAlias = sourceAlias;
        _targetAlias = targetAlias;
        _targetUiAlias = targetUiAlias;
        _removeKeys = [_multipleKey, .. removeKeys];
    }

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual string? Notice => null;

    public bool AppliesTo(XElement dataType, int folderMajorVersion)
    {
        if (folderMajorVersion >= _firstVersionWithSplit) return false;
        if (LegacyDataTypeXml.GetEditorAlias(dataType).Equals(_sourceAlias, StringComparison.OrdinalIgnoreCase) is false)
            return false;

        var config = LegacyDataTypeXml.GetConfig(dataType);

        // multiple: true stays, an untouched toggle (no key) or multiple: false is a single picker.
        return LegacyDataTypeXml.TryGetBoolean(config, _multipleKey, out var multiple) is false || multiple is false;
    }

    public SyncLegacyDataTypeMigration? MigrateDataType(XElement dataType)
    {
        var config = LegacyDataTypeXml.GetConfig(dataType);

        foreach (var existing in config.Select(x => x.Key).ToList())
        {
            if (_removeKeys.Contains(existing, StringComparer.OrdinalIgnoreCase))
                config.Remove(existing);
        }

        LegacyDataTypeXml.SetEditor(dataType, _targetAlias, _targetUiAlias, config);

        return new SyncLegacyDataTypeMigration(LegacyDataTypeXml.GetKey(dataType), _sourceAlias, _targetAlias, this);
    }
}

internal class MediaPickerSingleLegacyMigrator : SingleEditorLegacyMigrator
{
    public MediaPickerSingleLegacyMigrator()
        : base("Umbraco.MediaPicker3", "Umbraco.MediaPicker.Single", "Umb.PropertyEditorUi.MediaPicker.Single", "validationLimit")
    { }

    public override string Id => "mediaPickerSingle";
    public override string Name => "Single media pickers";
    public override string Description
        => "Media pickers that do not allow multiple items move to the Single Media Picker.";
}

internal class DropDownSingleLegacyMigrator : SingleEditorLegacyMigrator
{
    public DropDownSingleLegacyMigrator()
        : base("Umbraco.DropDown.Flexible", "Umbraco.DropDown.Single", "Umb.PropertyEditorUi.Dropdown.Single")
    { }

    public override string Id => "dropDownSingle";
    public override string Name => "Single dropdowns";
    public override string Description
        => "Dropdowns that do not allow multiple choices move to the Single Dropdown.";
}
