using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using uSync.Core.Extensions;

namespace uSync.Core.DataTypes.DataTypeSerializers;
internal class MediaPickerConfigSerializer : PickerConfigurationSerializerBase<IMediaType>, IConfigurationSerializer
{
    private readonly IMediaTypeService _mediaTypeService;

    public MediaPickerConfigSerializer(IMediaTypeService mediaTypeService)
    {
        _mediaTypeService = mediaTypeService;
    }

    public string Name => nameof(MediaPickerConfigSerializer);

    public string[] Editors => [
        "Umbraco.MediaPicker",
        "Umbraco.MediaPicker2",
        "Umbraco.MediaPicker3"];

    /// <summary>
    ///  we migrate this one, from "Umbraco.MediaPicker" to "Umbraco.MediaPicker3" 
    /// </summary>
    public string? GetEditorAlias()
        => Constants.PropertyEditors.Aliases.MediaPicker3;

    protected override IMediaType? GetByAlias(string alias)
        => _mediaTypeService.Get(alias);
}
