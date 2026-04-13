using System.Collections.Concurrent;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

using uSync.Core.Cache;
using uSync.Core.Extensions;
using uSync.Core.Mapping.Tracking;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Core.Mapping;

public class SyncValueMapperCollection
        : BuilderCollectionBase<ISyncMapper>
{
    private readonly SyncMapperTrackerCollection _mapperTrackers;

    private readonly ConcurrentDictionary<string, string> _customMappings = new(StringComparer.InvariantCultureIgnoreCase);
 
    public SyncEntityCache EntityCache { get; private set; }

    public SyncValueMapperCollection(
        SyncEntityCache entityCache,
        Func<IEnumerable<ISyncMapper>> items,
        SyncMapperTrackerCollection mapperTrackers)
        : base(items)
    {
        EntityCache = entityCache;

        // todo, load these from config. 
        _customMappings = [];
        _mapperTrackers = mapperTrackers;
    }

    /// <summary>
    ///  Returns the syncMappers associated with the propertyEditorAlias
    /// </summary>
    public IEnumerable<ISyncMapper> GetSyncMappers(string editorAlias)
    {
        var mappedAlias = GetMapperAlias(editorAlias);
        return this.Where(m => m.IsMapper(mappedAlias));
    }

    /// <summary>
    ///  will get any mappers and any mappers that are tracked for the editor alias, this is important because
    ///  this allows us to support old mappers for a property editor, even if the property editor alias has changed.
    /// </summary>
    public async Task<IEnumerable<ISyncMapper>> GetImportingSyncMappers(string editorAlias)
    {
        var mappers = await _mapperTrackers.GetMappersAsync(editorAlias);
        return [.. mappers, .. GetSyncMappers(editorAlias)];
    }

    /// <summary>
    ///  Get the mapped export value
    /// </summary>
    public async Task<string> GetExportValueAsync(object value, string editorAlias)
    {
        if (value is null) return string.Empty;

        var mappers = GetSyncMappers(editorAlias);
        if (mappers.Any())
        {
            var mappedValue = value.ToString() ?? string.Empty;

            foreach (var mapper in mappers)
            {
                mappedValue = await mapper.GetExportValueAsync(mappedValue ?? string.Empty, editorAlias);
            }

            return mappedValue ?? string.Empty;
        }

        return GetSafeValue(value);
    }

    /// <summary>
    ///  Get the mapped import value
    /// </summary>
    [Obsolete("Use GetImportValueAsync(string value, IPropertyType propertyType) instead will be removed in v19")]
    public async Task<object?> GetImportValueAsync(string value, string editorAlias)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
         
        var mappers = await GetImportingSyncMappers(editorAlias);
        if (mappers.Any())
        {
            var mappedValue = value;
            foreach (var mapper in mappers)
            {
                mappedValue = await mapper.GetImportValueAsync(mappedValue ?? string.Empty, editorAlias);
            }

            return GetCleanFlatJson(mappedValue ?? string.Empty);
        }

        return value;
    }

    public async Task<object?> GetImportValueAsync(string value, IPropertyType propertyType, SyncSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var mappers = await GetImportingSyncMappers(propertyType.PropertyEditorAlias);
        if (mappers.Any())
        {
            var mappedValue = value;
            foreach (var mapper in mappers)
            {
                mappedValue = 
                    mapper is ISyncPropertyMapper syncPropertyMapper ?
                     (await syncPropertyMapper.GetImportValueAsync(mappedValue ?? string.Empty, propertyType, options)) :
                     (await mapper.GetImportValueAsync(mappedValue ?? string.Empty, propertyType.PropertyEditorAlias, options));
            }

            return GetCleanFlatJson(mappedValue ?? string.Empty);
        }

        return value;
    }


    static readonly char[] _trimChars = ['\"', '\''];

    /// <summary>
    ///  cleans and flattens the JSON , so the stuff we import doesn't actually have all the spaces in it. 
    /// </summary>
    private static string GetCleanFlatJson(string stringValue)
    {
        // fix #749 be less aggressive about cleaning up json (let strings be strings)
        if (stringValue.TryParseToJsonNode(out var result) is false || result is null)
            return stringValue;

        if (result.TrySerializeJsonNode(out var jsonString, indent: false) is true)
            return jsonString.Trim(_trimChars);

        return stringValue.Trim(_trimChars);
    }

    /// <summary>
    ///  Ensure we get a globally portable string for a value
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   it should be the responsibility of the mapper to do this
    ///   but there are times (such as dates and times) when its 
    ///   better to ensure all values of a certain type leave 
    ///   using the same format. 
    ///  </para>
    /// </remarks>
    private static string GetSafeValue(object value)
    {
        return value switch
        {
            DateTime date => date.ToString("s"),
            _ => value.ToString() ?? "",
        };
    }

    /// <summary>
    ///  looks up the alias for a mapper (replacing it from settings if need be)
    /// </summary>
    private string GetMapperAlias(string alias)
        => _customMappings.TryGetValue(alias, out var mappedAlias) ? mappedAlias : alias;
}

public class SyncValueMapperCollectionBuilder
    : WeightedCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
    // : LazyCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
{
    protected override SyncValueMapperCollectionBuilder This => this;
}
