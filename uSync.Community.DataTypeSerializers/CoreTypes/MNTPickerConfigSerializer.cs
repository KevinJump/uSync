using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes;
using uSync.Core.Extensions;

namespace uSync.Community.DataTypeSerializers.CoreTypes;

public class MNTPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
{
    public MNTPickerConfigSerializer(IEntityService entityService)
        : base(entityService)
    { }

    public string Name => "MNTPNodeSerializer";

    public string[] Editors => ["Umbraco.MultiNodeTreePicker"];

    private const string _startNodeKey = "startNode";
    private const string _idKey = "id";
    private const string _dynamicRootKey = "dynamicRoot";
    private const string _originKeyKey = "originKey";

    public override IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue(_startNodeKey, out var startNodeValue)
            && startNodeValue is not null
            && startNodeValue.TryConvertToJsonObject(out var startNode))
        {
            TryMapNodeValue(startNode, _idKey, TryGuidToEntityPath);

            if (startNode.TryGetPropertyAsObject(_dynamicRootKey, out var dynamicRoot))
                TryMapNodeValue(dynamicRoot, _originKeyKey, TryGuidToEntityPath);

            configuration[_startNodeKey] = startNode;
        }

        return base.GetConfigurationExport(configuration);
    }

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue(_startNodeKey, out var startNodeValue)
            && startNodeValue is not null
            && startNodeValue.TryConvertToJsonObject(out var startNode))
        {
            TryMapNodeValue(startNode, _idKey, TryPathToGuid);

            if (startNode.TryGetPropertyAsObject(_dynamicRootKey, out var dynamicRoot))
                TryMapNodeValue(dynamicRoot, _originKeyKey, TryPathToGuid);

            configuration[_startNodeKey] = startNode;
        }

        return base.GetConfigurationImport(configuration);
    }

    private delegate bool TryConvert<TResult>(Guid guid, out TResult result);
    private delegate bool TryConvertBack(string value, out Guid guid);

    /// <summary>
    ///  maps a guid property value to its entity path, if the property is present and holds a guid.
    /// </summary>
    private static bool TryMapNodeValue(JsonObject node, string propertyName, TryConvert<string> converter)
    {
        if (node.TryGetPropertyValue(propertyName, out var propertyValue) is false
            || propertyValue is null) return false;

        if (Guid.TryParse(propertyValue.ToString(), out var guid) is false) return false;
        if (converter(guid, out var path) is false) return false;

        node[propertyName] = path;
        return true;
    }

    /// <summary>
    ///  maps an entity path property value back to a guid, if the property is present and holds a path.
    /// </summary>
    private static bool TryMapNodeValue(JsonObject node, string propertyName, TryConvertBack converter)
    {
        if (node.TryGetPropertyValue(propertyName, out var propertyValue) is false
            || propertyValue is null) return false;

        if (converter(propertyValue.ToString(), out var guid) is false) return false;

        node[propertyName] = guid;
        return true;
    }
}
