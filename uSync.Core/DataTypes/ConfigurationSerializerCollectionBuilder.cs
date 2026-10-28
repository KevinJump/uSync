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
    ///  find the first serializer that wants to move this configuration onto another editor.
    /// </summary>
    public SyncEditorReplacement? GetEditorReplacement(string editorAlias, IDictionary<string, object> configuration)
    {
        foreach (var serializer in GetSerializers(editorAlias))
        {
            var replacement = serializer.GetEditorReplacement(editorAlias, configuration);
            if (replacement is not null)
                return replacement;
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
