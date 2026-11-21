using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Mapping.Mappers;
internal class BlockGridMapper : SyncBlockMapperBase<BlockGridValue>, ISyncMapper
{
    public override string Name => "NuBlock Grid Mapper";
    public override string[] Editors { get; } = [Constants.PropertyEditors.Aliases.BlockGrid];

    public BlockGridMapper(
        IEntityService entityService,
        IContentTypeService contentTypeService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        ILogger<BlockGridMapper> logger) 
        : base(entityService, contentTypeService, mapperCollection, logger)
    {  }
}
