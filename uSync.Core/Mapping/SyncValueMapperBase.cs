using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Serialization;

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

    public virtual bool IsMapper(string editorAlias)
        => Editors.InvariantContains(editorAlias);

    public virtual bool IsMapper(PropertyType propertyType)
        => Editors.InvariantContains(propertyType.PropertyEditorAlias);

    public virtual Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
        => Task.FromResult(Enumerable.Empty<uSyncDependency>());

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

    [Obsolete("Use GetImportValueAsync(string value, string editorAlias, SyncSerializerOptions options) instead. will be removed in v19")]
    public virtual Task<string?> GetImportValueAsync(string value, string editorAlias)
        => GetImportValueAsync(value, editorAlias, new SyncSerializerOptions());

    public virtual Task<string?> GetImportValueAsync(string value, IPropertyType propertyType, SyncSerializerOptions options)
        => Task.FromResult<string?>(value);

    public virtual Task<string?> GetImportValueAsync(string value, string editorAlias, SyncSerializerOptions options)
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
        return value.TryConvertPreChecked<TObject>(out var result) ? result : default;
    }
}

/// <summary>
///  dependent value mappers, require other property editors to be present to work. 
///  if the other property editor is not installed, this mapper will not run for the property. 
/// </summary>
public abstract class SyncDependentValueMapperBase : SyncValueMapperBase
{
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly string? _requiresEditor;

    protected SyncDependentValueMapperBase(
        IEntityService entityService,
        PropertyEditorCollection propertyEditors) : base(entityService)
    {
        var requires = GetType().GetCustomAttribute<RequiresPropertyEditorAttribute>(false);
        if (requires != null)
            _requiresEditor = requires.Editor;
        _propertyEditors = propertyEditors;
    }

    public override bool IsMapper(PropertyType propertyType)
    {
        return base.IsMapper(propertyType) &&
            _propertyEditors.Any(x => x.Alias.Equals(_requiresEditor, StringComparison.InvariantCultureIgnoreCase));
    }
}