using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Mapping;

namespace uSync.Community.Contrib.Mappers;

/// <summary>
///  value mapper for Contentment content blocks. 
/// </summary>
public class ContentmentContentBlocks : SyncNestedJsonValueMapperBase, ISyncMapper
{
    public ContentmentContentBlocks(IEntityService entityService, Lazy<SyncValueMapperCollection> mapperCollection, IContentTypeService contentTypeService, IDataTypeService dataTypeService)
        : base(entityService, mapperCollection, contentTypeService, dataTypeService)
    { }

    public override string Name => "Contentment content block mapper";

    public override string[] Editors => [
        "Umbraco.Community.Contentment.ContentBlocks"
    ];

    protected override async Task<string?> ProcessValuesAsync(JsonObject jsonValue, string editorAlias, Func<JsonObject, IContentType, Task<JsonObject>> GetPropertiesMethod)
    {
        var elements = jsonValue.AsArray();

        foreach (var item in elements.AsListOfJsonObjects())
        {
            var itemValue = item?.GetPropertyAsObject("value");
            if (itemValue is null) continue;

            var doctype = await GetDocTypeByKeyAsync(item, "elementType");
            if (doctype is null) continue;

            await GetImportPropertiesAsync(itemValue, doctype);
        }

        return elements.SerializeJsonNode(false);

    }

    public override async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        var stringValue = GetValueAs<string>(value);

        if (stringValue.TryParseToJsonArray(out JsonArray? elements) is false || elements is null) return [];
        if (elements == null || elements.Count == 0) return [];

        var dependencies = new List<uSyncDependency>();

        foreach (var item in elements.AsListOfJsonObjects())
        {
            if (item is null) continue;

            var itemValue = item.GetPropertyAsObject("value");
            if (itemValue == null) continue;

            var doctype = await GetDocTypeByKeyAsync(item, "elementType");
            if (doctype == null) continue;

            if (flags.HasFlag(DependencyFlags.IncludeDependencies))
            {
                var doctypeDependency = await CreateDocTypeDependencyAsync(doctype.Alias, flags);
                if (doctypeDependency != null) dependencies.Add(doctypeDependency);
            }

            dependencies.AddRange(await GetPropertyDependenciesAsync(itemValue, doctype, flags));
        }

        return dependencies;
    }
}
