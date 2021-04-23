using System;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Serialization
{

    public abstract class SyncSerializerBase<TObject> : SyncSerializerRoot<TObject>
        where TObject : IEntity
    {
        protected readonly IEntityService entityService;

        protected SyncSerializerBase(IEntityService entityService, ILogger<SyncSerializerBase<TObject>> logger)
            : base(logger)
        {
            this.entityService = entityService;
        }

        public override Guid ItemKey(TObject item)
            => item.Key;
    }
}
