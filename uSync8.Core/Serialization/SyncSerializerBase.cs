using System;

using Umbraco.Core.Logging;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

namespace uSync8.Core.Serialization
{
    public abstract class SyncSerializerBase<TObject> : SyncSerializerRoot<TObject, int>
        where TObject : IEntity
    {
        protected readonly IEntityService entityService;
        protected SyncSerializerBase(IEntityService entityService, ILogger logger)
            : base(logger)
        {
            this.entityService = entityService;
        }

        protected override Guid ItemKey(TObject item) => item.Key;
        protected override int ItemId(TObject item) => item.Id;

    }
}
