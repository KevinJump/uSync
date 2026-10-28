using Jumoo.Json;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;

namespace uSync.Core.Mapping;

/// <summary>
///  Mapper for the v19 pickers that store a JSON array of keys
///  (e.g ["c0a3b4e2-..."]) rather than a list of udis.
/// </summary>
/// <remarks>
///  the keys don't need mapping between sites (uSync keeps keys in step),
///  but we need them for the dependencies.
/// </remarks>
[NullableMapper]
public class KeyPickerMapper : SyncValueMapperBase, ISyncMapper
{
    public KeyPickerMapper(IEntityService entityService) : base(entityService)
    {
    }

    public override string Name => "Key Picker Mapper";

    public override string[] Editors => [
        Constants.PropertyEditors.Aliases.MultipleDocumentPicker,
        Constants.PropertyEditors.Aliases.MultipleMemberPicker
    ];

    public override Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            if (value?.ToString().TryDeserialize<List<Guid>>(out var keys) is not true || keys is null)
                return [];

            var entityType = editorAlias == Constants.PropertyEditors.Aliases.MultipleMemberPicker
                ? Constants.UdiEntityType.Member
                : Constants.UdiEntityType.Document;

            return keys
                .Select(key => CreateDependency(Udi.Create(entityType, key) as GuidUdi, flags))
                .OfType<uSyncDependency>();
        });
    }
}
