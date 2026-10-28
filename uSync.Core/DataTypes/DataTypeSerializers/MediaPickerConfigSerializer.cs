using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using uSync.Core.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;
internal class MediaPickerConfigSerializer : PickerConfigurationSerializerBase<IMediaType>, IConfigurationSerializer
{
    private const string _multipleKey = "multiple";

    private readonly IMediaTypeService _mediaTypeService;

    public MediaPickerConfigSerializer(IMediaTypeService mediaTypeService)
    {
        _mediaTypeService = mediaTypeService;
    }

    public string Name => nameof(MediaPickerConfigSerializer);

    public string[] Editors => [
        "Umbraco.MediaPicker",
        "Umbraco.MediaPicker2",
        "Umbraco.MediaPicker3",
        Constants.PropertyEditors.Aliases.SingleMediaPicker];

    /// <summary>
    ///  we migrate this one, from "Umbraco.MediaPicker" to "Umbraco.MediaPicker3"
    /// </summary>
    public string? GetEditorAlias()
        => Constants.PropertyEditors.Aliases.MediaPicker3;

    /// <summary>
    ///  v19 moves media pickers with multiple:false onto the single media picker.
    /// </summary>
    /// <remarks>
    ///  Umbraco's own migration also treats a missing 'multiple' as single, but v19 no
    ///  longer writes the key for a multiple picker, so we only move it when it's set.
    /// </remarks>
    public SyncEditorReplacement? GetEditorReplacement(string editorAlias, IDictionary<string, object> configuration)
        => IsSingleMediaPicker(editorAlias, configuration)
            ? new(Constants.PropertyEditors.Aliases.SingleMediaPicker, "Umb.PropertyEditorUi.MediaPicker.Single")
            : null;

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        var result = base.GetConfigurationImport(configuration);

        // the single picker holds one item, so it has no item count to validate.
        if (IsSingleMediaPicker(Constants.PropertyEditors.Aliases.MediaPicker3, result))
            result = result.RemoveKeys("validationLimit");

        return result.RemoveKeys(_multipleKey);
    }

    private static bool IsSingleMediaPicker(string editorAlias, IDictionary<string, object> configuration)
        => editorAlias == Constants.PropertyEditors.Aliases.MediaPicker3
            && configuration.TryGetBoolean(_multipleKey, out var multiple) && multiple is false;

    protected override IMediaType? GetByAlias(string alias)
        => _mediaTypeService.Get(alias);
}
