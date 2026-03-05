using Umbraco.Cms.Core.Services;

using uSync.Core.Mapping;

namespace uSync.Community.Migrations.SpectrumColourPicker;

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