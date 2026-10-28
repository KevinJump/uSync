using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class SliderMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    private const string _enableRangeKey = "enableRange";

    public string Name => nameof(SliderMigratingConfigSerializer);
    public string[] Editors => [
        Constants.PropertyEditors.Aliases.Slider,
        Constants.PropertyEditors.Aliases.RangeSlider];

    /// <summary>
    ///  v19 moves sliders with enableRange:true onto their own range slider editor.
    /// </summary>
    public SyncEditorReplacement? GetEditorReplacement(string editorAlias, IDictionary<string, object> configuration)
        => editorAlias == Constants.PropertyEditors.Aliases.Slider
            && configuration.TryGetBoolean(_enableRangeKey, out var enableRange) && enableRange
            ? new(Constants.PropertyEditors.Aliases.RangeSlider, "Umb.PropertyEditorUi.Slider.Range")
            : null;

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
        => MigratePropertyNames(configuration, new()
        {
            { "initialValue", "initVal1" },
            { "initialValue2", "initVal2" },
            { "maximumValue", "maxVal" },
            { "minimumValue", "minVal" },
            { "stepIncrements", "step" }
        })
        // v19: the range is the editor now, and min/max are a single range value.
        // a zero minimum is a real lower bound on a slider, a zero maximum means no maximum.
        .RemoveKeys(_enableRangeKey)
        .MigrateMinMaxToRange("minVal", "maxVal", "validationRange", minZeroIsUnbounded: false, maxZeroIsUnbounded: true);
}
