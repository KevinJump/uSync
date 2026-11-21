using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Mapping.Mappers;

internal class BlockListMapper : SyncBlockMapperBase<BlockListValue>, ISyncMapper
{
    public override string Name => "NuBlock List mapper";

    public override string[] Editors { get; } = [Constants.PropertyEditors.Aliases.BlockList];

    public BlockListMapper(
        IEntityService entityService,
        IContentTypeService contentTypeService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        ILogger<BlockListMapper> logger)
        : base(entityService, contentTypeService, mapperCollection, logger)
    {
    }

}
