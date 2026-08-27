using System;
using System.Collections.Generic;

using Umbraco.Cms.Core.Services;

using uSync.Community.DataTypeSerializers;
using uSync.Core.DataTypes;

namespace uSync.Community.DataTypeSerializers.CoreTypes;

public class ContentPickerConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
{
    public ContentPickerConfigSerializer(IEntityService entityService)
        : base(entityService)
    { }

    public string Name => "ContentPickerNodeSerializer";
    public string[] Editors => ["Umbraco.ContentPicker"];

    private const string _startNodeIdKey = "startNodeId";

    public override IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue(_startNodeIdKey, out var startNodeId)
            && Guid.TryParse(startNodeId.ToString(), out var startNodeGuid)
            && TryGuidToEntityPath(startNodeGuid, out var entityPath))
        {
            configuration["startNodeId"] = entityPath;
        }

        return base.GetConfigurationExport(configuration);
    }

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue(_startNodeIdKey, out var startNodeId)
            && startNodeId is string startNodePath
            && TryPathToGuid(startNodePath, out var startNodeGuid))
        {
            configuration["startNodeId"] = startNodeGuid;
        }

        return base.GetConfigurationImport(configuration);
    }
}
