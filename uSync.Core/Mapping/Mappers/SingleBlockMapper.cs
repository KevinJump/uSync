using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Mapping.Mappers;

internal class SingleBlockMapper : SyncBlockMapperBase<SingleBlockValue>, ISyncMapper
{
    public override string Name => "NuBlock Single Block mapper";

    public override string[] Editors => [Constants.PropertyEditors.Aliases.SingleBlock];

    public SingleBlockMapper(
        IEntityService entityService,
        IContentTypeService contentTypeService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        ILogger<SingleBlockMapper> logger)
        : base(entityService, contentTypeService, mapperCollection, logger)
    {
    }
}
