using System.Collections.Generic;
using Umbraco.Core.Composing;

namespace uSync8.Core.Serialization
{
    public class USyncSerializerCollectionBuilder
        : LazyCollectionBuilderBase<USyncSerializerCollectionBuilder, USyncSerializerCollection, ISyncSerializerBase>
    {
        protected override USyncSerializerCollectionBuilder This => this;
    }


    public class USyncSerializerCollection : BuilderCollectionBase<ISyncSerializerBase>
    {
        public USyncSerializerCollection(IEnumerable<ISyncSerializerBase> items)
            : base(items)
        { }
    }
}
