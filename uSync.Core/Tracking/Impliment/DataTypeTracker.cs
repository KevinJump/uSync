using System.Xml.Linq;

using Umbraco.Cms.Core.Models;

using uSync.Core.Extensions;
using uSync.Core.Roots.Configs;
using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class DataTypeTracker : SyncXmlTracker<IDataType>, ISyncTracker<IDataType>
{
    private readonly SyncConfigMergerCollection _configMergers;

    public DataTypeTracker(
        SyncSerializerCollection serializers,
        SyncConfigMergerCollection configMergers)
        : base(serializers)
    {
        _configMergers = configMergers;
    }

    public override List<TrackingItem> TrackingItems =>
    [
        TrackingItem.Single("Name", "/Info/Name"),
        TrackingItem.Single("EditorAlias", "/Info/EditorAlias"),
        TrackingItem.Single("Database Type", "/Info/DatabaseType"),
        TrackingItem.Single("Sort Order", "/Info/SortOrder"),
        TrackingItem.Single("Folder", "/Info/Folder"),
        TrackingItem.Single("Config", "/Config")
    ];

    public override XElement MergeFiles(XElement a, XElement b)
    {
        var editorAlias = GetEditorAlias(a);
        var merger = GetConfigMerger(editorAlias);
        if (!string.IsNullOrEmpty(editorAlias) && merger != null)
        {
            var rootConfig = a.Element("Config").ValueOrDefault(string.Empty);
            var targetConfig = b.Element("Config").ValueOrDefault(string.Empty);

            if (!string.IsNullOrEmpty(rootConfig) && !string.IsNullOrEmpty(targetConfig))
            {
                var mergedConfig = merger.GetMergedConfig(rootConfig, targetConfig);
                if (mergedConfig != null)
                {
                    b.Element("Config")?.ReplaceNodes(new XCData(SerializeConfig(mergedConfig)));
                }

                return b;
            }
            // merge configs. 
        }
        return base.MergeFiles(a, b);
    }

    public override XElement? GetDifferences(List<XElement> nodes)
    {
        if (nodes.Count <= 1) return base.GetDifferences(nodes);

        var editorAlias = GetEditorAlias(nodes[0]);
        var merger = GetConfigMerger(editorAlias);
        if (!string.IsNullOrEmpty(editorAlias) && merger != null)
        {
            return GetDifferences(nodes[0], nodes[1], merger);
        }

        return base.GetDifferences(nodes);
    }

    public XElement? GetDifferences(XElement root, XElement target, ISyncConfigMerger merger)
    {

        var rootConfig = root.Element("Config").ValueOrDefault(string.Empty);
        var targetConfig = target.Element("Config").ValueOrDefault(string.Empty);

        if (!string.IsNullOrEmpty(rootConfig) && !string.IsNullOrEmpty(targetConfig))
        {
            // calculate config differences. 
            var difference = merger.GetDifferenceConfig(rootConfig, targetConfig);
            if (difference != null)
            {
                root.Element("Config")?.ReplaceNodes(new XCData(SerializeConfig(difference)));

                return root;
            }

        }

        return base.GetDifferences([root, target]);
    }

    private string GetEditorAlias(XElement node)
        => node.Element("Info")?.Element("EditorAlias").ValueOrDefault(string.Empty) ?? string.Empty;


    private ISyncConfigMerger? GetConfigMerger(string editorAlias)
        => _configMergers?.GetConfigMerger(editorAlias) ?? null;

    private static string SerializeConfig(object config)
        => config.SerializeJsonString();


}
