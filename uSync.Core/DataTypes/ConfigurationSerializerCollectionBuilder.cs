using System.Collections.Generic;
using System.Linq;

using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

namespace uSync.Core.DataTypes
{
    public class ConfigurationSerializerCollectionBuilder
        : LazyCollectionBuilderBase<ConfigurationSerializerCollectionBuilder, ConfigurationSerializerCollection, IConfigurationSerializer>
    {
        protected override ConfigurationSerializerCollectionBuilder This => this;
    }


    public class ConfigurationSerializerCollection :
        BuilderCollectionBase<IConfigurationSerializer>
    {
        public ConfigurationSerializerCollection(IEnumerable<IConfigurationSerializer> items)
            : base(items)
        {
        }

        public IConfigurationSerializer GetSerializer(string editorAlias)
        {
            return this.FirstOrDefault(x => x.Editors.InvariantContains(editorAlias));
        }
    }
}
