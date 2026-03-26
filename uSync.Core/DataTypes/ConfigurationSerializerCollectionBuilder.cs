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

    [Obsolete("Use GetSerializers instead to get all serializers for an editor alias, getting only the first is not recommended. will be removed in v19")]
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

    /// <summary>
    ///  tells serializers that care about it that this is a rename 
    /// </summary>
    /// <param name="oldEditorAlias"></param>
    /// <param name="newEditorAlias"></param>
    public async Task TrackRenamedEditorAsync(string oldEditorAlias, string newEditorAlias) {
    
        foreach(var serializer in GetSerializers(oldEditorAlias))
        {
            if (serializer is IConfigurationTrackingSerializer trackingSerializer)
                await trackingSerializer.TrackRenamedEditorAsync(oldEditorAlias, newEditorAlias);
        }

    }
}
