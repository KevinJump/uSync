using System;
using System.Collections.Generic;

using Umbraco.Cms.Core.Services;

using uSync.Community.DataTypeSerializers;
using uSync.Core.DataTypes;

namespace uSync.Community.DataTypeSerializers.CoreTypes;

public class RichTextConfigSerializer : SyncDataTypeSerializerBase, IConfigurationSerializer
{
    public RichTextConfigSerializer(IEntityService entityService)
        : base(entityService)
    { }

    public string Name => "RichTextNodeSerializer";

    public string[] Editors => ["Umbraco.RichText"];

    private const string _mediaParentIdKey = "mediaParentId";

    public override IDictionary<string, object> GetConfigurationExport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue(_mediaParentIdKey, out var mediaParentId)
            && Guid.TryParse(mediaParentId?.ToString(), out var mediaParentGuid)
            && TryGuidToEntityPath(mediaParentGuid, out var entityPath))
        {
            // other serializers for this editor (e.g. uSync.Core's RichTextEditorMigratingSerializer)
            // may hand us a read-only dictionary, so copy before mutating.
            configuration = new Dictionary<string, object>(configuration)
            {
                [_mediaParentIdKey] = entityPath
            };
        }

        return base.GetConfigurationExport(configuration);
    }

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue(_mediaParentIdKey, out var mediaParentId)
            && mediaParentId is string mediaParentPath
            && TryPathToGuid(mediaParentPath, out var mediaParentGuid))
        {
            // other serializers for this editor (e.g. uSync.Core's RichTextEditorMigratingSerializer)
            // may hand us a read-only dictionary, so copy before mutating.
            configuration = new Dictionary<string, object>(configuration)
            {
                [_mediaParentIdKey] = mediaParentGuid
            };
        }

        return base.GetConfigurationImport(configuration);
    }
}
