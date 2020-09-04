using System;

using Umbraco.Core.Logging;
using Umbraco.Core.Models.Entities;
using Umbraco.Core.Services;

namespace uSync.Core.Serialization
{

    public abstract class SyncSerializerBase<TObject> : SyncSerializerRoot<TObject>
        where TObject : IEntity
    {
        protected readonly IEntityService entityService;

        protected SyncSerializerBase(IEntityService entityService, ILogger logger)
            : base(logger)
        {
            this.entityService = entityService;
        }

        protected override Guid ItemKey(TObject item)
            => item.Key;
    }
}
