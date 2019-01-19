using System.Collections.Generic;
using Umbraco.Core.Composing;

namespace uSync8.Core.Serialization
{
    public class USyncSerializerCollectionBuilder
        : LazyCollectionBuilderBase<USyncSerializerCollectionBuilder, USyncSerializerCollection, IUSyncSerializer>
    {
        protected override USyncSerializerCollectionBuilder This => this;
    }


    public class USyncSerializerCollection : BuilderCollectionBase<IUSyncSerializer>
    {
        public USyncSerializerCollection(IEnumerable<IUSyncSerializer> items)
            : base(items)
        { }
    }
}
