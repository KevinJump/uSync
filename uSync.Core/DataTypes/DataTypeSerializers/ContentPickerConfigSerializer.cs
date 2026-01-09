using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class ContentPickerConfigSerializer : PickerConfigurationSerializerBase<IContentType>, IConfigurationSerializer
{
    private readonly IContentTypeService _contentTypeService;
    public ContentPickerConfigSerializer(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
    }
    public string Name => nameof(ContentPickerConfigSerializer);
    public string[] Editors => [Constants.PropertyEditors.Aliases.ContentPicker];

    protected override IContentType? GetByAlias(string alias)
        => _contentTypeService.Get(alias);
}