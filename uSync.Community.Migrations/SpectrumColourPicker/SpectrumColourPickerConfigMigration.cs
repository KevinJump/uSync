
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;
using uSync.Core.Mapping;

namespace uSync.Community.Migrations.SpectrumColourPicker;

/// <summary>
///  migrate the datatype for a "Spectrum.Color.Picker" to a ColourPickerEyeDropper datatype, which is the new name for the same editor.
/// </summary>
public class SpectrumColourPickerConfigMigration : ConfigurationSerializerBase, IConfigurationSerializer
{
    public string Name => nameof(SpectrumColourPickerConfigMigration);
    public string[] Editors => ["Spectrum.Color.Picker"];
    public string? GetEditorAlias() => Constants.PropertyEditors.Aliases.ColorPickerEyeDropper;
    public string? GetEditorUIAlias() => "Umb.PropertyEditorUi.EyeDropper";

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        var mappedConfiguration = MigratePropertyNames(configuration, new Dictionary<string, string>
        {
            { "enableTransparency", "showAlpha" },
        });

        if (configuration.TryGetValue("palette", out var value) && value is string palletValue)
            mappedConfiguration["palette"] = !string.IsNullOrWhiteSpace(palletValue);

        return mappedConfiguration;
    }
}

/// <summary>
///  migrate specrum colour picker content values to eyedropper values.
/// </summary>
public class SpectrumColourPickerContentMigration : SyncValueMapperBase, ISyncMapper
{
    public SpectrumColourPickerContentMigration(IEntityService entityService) 
        : base(entityService)
    { }

    public override string Name => nameof(SyncValueMapperBase);
    public override string[] Editors => ["Spectrum.Color.Picker"];

    public override Task<string?> GetImportValueAsync(string value, string editorAlias)
    {
        if (string.IsNullOrWhiteSpace(value)) return Task.FromResult<string?>(value);
        if (value.StartsWith('#')) return Task.FromResult<string?>(value);

        if (value.Length == 6)
            return Task.FromResult<string?>($"#{value}");

        return Task.FromResult<string?>(value);
    }
}