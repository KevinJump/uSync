using Umbraco.Cms.Core.Composing;
using Umbraco.Extensions;

using uSync.Core.Cache;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

public class SyncValueMapperCollection
        : BuilderCollectionBase<ISyncMapper>
{
    private readonly Dictionary<string, string> _customMappings;

    public SyncEntityCache EntityCache { get; private set; }

    public SyncValueMapperCollection(
        SyncEntityCache entityCache,
        Func<IEnumerable<ISyncMapper>> items)
        : base(items)
    {
        EntityCache = entityCache;

        // todo, load these from config. 
        _customMappings = new Dictionary<string, string>();
    }

    /// <summary>
    ///  Returns the syncMappers associated with the propertyEditorAlias
    /// </summary>
    public IEnumerable<ISyncMapper> GetSyncMappers(string editorAlias)
    {
        var mappedAlias = GetMapperAlias(editorAlias);
        return this.Where(x => x.Editors.InvariantContains(mappedAlias));
    }

    /// <summary>
    ///  Get the mapped export value
    /// </summary>
    public string GetExportValue(object value, string editorAlias)
    {
        if (value == null) return string.Empty;

        var mappers = GetSyncMappers(editorAlias);
        if (mappers.Any())
        {
            var mappedValue = value.ToString() ?? "";
            foreach (var mapper in mappers)
            {
                mappedValue = mapper.GetExportValue(mappedValue, editorAlias);
            }

            return mappedValue;
        }

        return GetSafeValue(value);
    }

    /// <summary>
    ///  Get the mapped import value
    /// </summary>
    public object? GetImportValue(string value, string editorAlias)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var mappers = GetSyncMappers(editorAlias);
        if (mappers.Any())
        {
            var mappedValue = value;
            foreach (var mapper in mappers)
            {
                mappedValue = mapper.GetImportValue(mappedValue, editorAlias);
            }

            return GetCleanFlatJson(mappedValue);
        }

        return value;
    }

    /// <summary>
    ///  cleans and flattens the JSON , so the stuff we import doesn't actually have all the spaces in it. 
    /// </summary>
    private static string GetCleanFlatJson(string stringValue)
    {
        if (stringValue.TryConvertToJsonNode(out var result) is false || result is null)
            return stringValue;

        if (result.TrySerializeJsonNode(out var jsonString, indent: false) is true)
            return jsonString;

        return stringValue;
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
    {
        if (_customMappings.ContainsKey(alias.ToLower()))
            return _customMappings[alias.ToLower()];

        return alias;
    }
}

public class SyncValueMapperCollectionBuilder
    // : WeightedCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
    : LazyCollectionBuilderBase<SyncValueMapperCollectionBuilder, SyncValueMapperCollection, ISyncMapper>
{
    protected override SyncValueMapperCollectionBuilder This => this;
}
