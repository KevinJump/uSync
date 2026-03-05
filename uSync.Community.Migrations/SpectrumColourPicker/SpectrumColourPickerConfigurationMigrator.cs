
using Umbraco.Cms.Core;

using uSync.Core.DataTypes;

namespace uSync.Community.Migrations.SpectrumColourPicker;

/// <summary>
///  migrate the datatype for a "Spectrum.Color.Picker" to a ColourPickerEyeDropper datatype, which is the new name for the same editor.
/// </summary>
public class SpectrumColourPickerConfigurationMigrator : SyncConfigurationMigratorBase, IConfigurationSerializer
{
    public string Name => nameof(SpectrumColourPickerConfigurationMigrator);
    public string[] Editors => ["Spectrum.Color.Picker"];

    public override string? TargetEditor => Constants.PropertyEditors.Aliases.ColorPickerEyeDropper;

    public override IDictionary<string, object> GetMigratedConfiguration(IDictionary<string, object> configuration)
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
