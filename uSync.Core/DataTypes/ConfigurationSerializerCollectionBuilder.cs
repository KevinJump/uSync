using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

namespace uSync.Core.DataTypes;

public class ConfigurationSerializerCollectionBuilder
    : WeightedCollectionBuilderBase<ConfigurationSerializerCollectionBuilder, ConfigurationSerializerCollection, IConfigurationSerializer>
{
    protected override ConfigurationSerializerCollectionBuilder This => this;
}


public class ConfigurationSerializerCollection :
    BuilderCollectionBase<IConfigurationSerializer>
{
    public ConfigurationSerializerCollection(Func<IEnumerable<IConfigurationSerializer>> items)
        : base(items)
    {
    }

    public IConfigurationSerializer? GetSerializer(string editorAlias)
        => this.FirstOrDefault(x => x.IsSerializer(editorAlias));

    public IEnumerable<IConfigurationSerializer> GetSerializers(string editorAlias)
        => this.Where(x => x.IsSerializer(editorAlias));

    /// <summary>
    ///  find the first serializer that returns a non-null UI alias.
    /// </summary>
    public string? GetEditorUIAlias(string editorAlias)
    {
        foreach (var serializer in GetSerializers(editorAlias))
        {
            var uiAlias = serializer.GetEditorUIAlias();
            if (uiAlias.IsNullOrWhiteSpace() is false)
            {
                return uiAlias;
            }
        }
        return null;
    }
}
