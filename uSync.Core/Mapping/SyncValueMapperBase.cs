using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

public abstract class SyncValueMapperBase
{
    protected readonly IEntityService entityService;

    private readonly bool _hasNullableValue = false;

    public SyncValueMapperBase(IEntityService entityService)
    {
        this.entityService = entityService;

        var meta = GetType().GetCustomAttribute<NullableMapperAttribute>(false);
        if (meta != null) _hasNullableValue = true;

    }

    public abstract string Name { get; }

    public abstract string[] Editors { get; }

    public virtual bool IsMapper(PropertyType propertyType)
        => Editors.InvariantContains(propertyType.PropertyEditorAlias);

    [Obsolete("use GetDependenciesAsync will be removed in v16")]
    public virtual IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        => GetDependenciesAsync(value, editorAlias, flags).Result;

    public virtual Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
        => Task.FromResult(Enumerable.Empty<uSyncDependency>());

    [Obsolete("use GetExportValueAsync will be removed in v16")]
    public virtual string? GetExportValue(object value, string editorAlias)
        => GetExportValueAsync(value, editorAlias).Result;
    public virtual Task<string?> GetExportValueAsync(object value, string editorAlias)
    {
        return uSyncTaskHelper.FromResultOf(() =>
        {
            // if this is a nullable value, we return null when its blank 
            if (_hasNullableValue && (value == null || (value is string valueString && string.IsNullOrWhiteSpace(valueString))))
                return null;

            // default behavior is to string - which returns "" when null. 
            return value.ToString();
        });
    }

    [Obsolete("use GetImportValueAsync will be removed in v16")]
    public virtual string? GetImportValue(string value, string editorAlias)
        => GetImportValueAsync(value, editorAlias).Result;
    
    public virtual Task<string?> GetImportValueAsync(string value, string editorAlias)
        => Task.FromResult<string?>(value);



    protected IEnumerable<uSyncDependency> CreateDependencies(IEnumerable<string> udiStrings, DependencyFlags flags)
    {
        if (udiStrings == null || !udiStrings.Any()) yield break;

        foreach (var udiString in udiStrings)
        {
            var dependency = CreateDependency(udiString, flags);
            if (dependency != null) yield return dependency;
        }
    }

    protected uSyncDependency? CreateDependency(string udiString, DependencyFlags flags)
    {
        if (UdiParser.TryParse<GuidUdi>(udiString, out GuidUdi? udi))
        {
            return CreateDependency(udi, flags);
        }

        return null;
    }

    protected uSyncDependency? CreateDependency(GuidUdi? udi, DependencyFlags flags)
    {
        if (udi == null) return null;

        var entity = GetElement(udi);

        return new uSyncDependency()
        {
            Name = entity?.Name ?? udi.ToString() ?? string.Empty,
            Udi = udi,
            Flags = flags,
            Order = DependencyOrders.OrderFromEntityType(udi.EntityType),
            Level = entity == null ? 0 : entity.Level
        };
    }

    private IEntitySlim? GetElement(GuidUdi udi)
    {
        if (udi != null)
            return entityService.Get(udi.Guid);

        return null;
    }

    /// <summary>
    ///  helper to convert object to a string (with all the checks)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    protected static TObject? GetValueAs<TObject>(object value)
    {
        if (value == null) return default;
        var attempt = value.TryConvertTo<TObject>();
        if (!attempt) return default;

        return attempt.Result;
    }
}
