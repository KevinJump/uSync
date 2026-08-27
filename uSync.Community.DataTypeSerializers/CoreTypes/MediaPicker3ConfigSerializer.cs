using System;
using System.Collections.Generic;

using Umbraco.Cms.Core.Services;

using uSync.Community.DataTypeSerializers;
using uSync.Core.DataTypes;

namespace uSync.Community.DataTypeSerializers.CoreTypes;

public class MediaPicker3ConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
{
    public MediaPicker3ConfigSerializer(IEntityService entityService)
        : base(entityService)
    { }

    public string Name => "MediaPicker3NodeSerializer";

    public string[] Editors => ["Umbraco.MediaPicker3"];

    private const string _startNodeIdKey = "startNodeId";

    public override IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue(_startNodeIdKey, out var startNodeId)
            && Guid.TryParse(startNodeId?.ToString(), out var startNodeGuid)
            && TryGuidToEntityPath(startNodeGuid, out var entityPath))
        {
            configuration[_startNodeIdKey] = entityPath;
        }

        return base.GetConfigurationExport(configuration);
    }

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue(_startNodeIdKey, out var startNodeId)
            && startNodeId is string startNodePath
            && TryPathToGuid(startNodePath, out var startNodeGuid))
        {
            configuration[_startNodeIdKey] = startNodeGuid;
        }

        return base.GetConfigurationImport(configuration);
    }
}
