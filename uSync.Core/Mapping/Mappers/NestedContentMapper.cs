using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

public class NestedContentMapper : SyncNestedJsonValueMapperBase, ISyncMapper
{
    private readonly string _docTypeAliasValue = "ncContentTypeAlias";

    public NestedContentMapper(
        IEntityService entityService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService)
        : base(entityService, mapperCollection, contentTypeService, dataTypeService)
    { }

    public override string Name => "Nested Content Mapper";

    public override string[] Editors => ["Our.Umbraco.NestedContent", Constants.PropertyEditors.Aliases.NestedContent];

    protected override string? ProcessValues(JsonObject jsonValue, string editorAlias, Func<JsonObject, IContentType, JsonObject> GetPropertiesMethod)
    {
        if (jsonValue.GetValueKind() != JsonValueKind.Array)
        {
            jsonValue.TrySerializeJsonNode(out var baseResult, true);
            return baseResult;
        }

        var nestedJson = jsonValue.ToArray();
        foreach (var item in nestedJson.Select(x => x.Value?.AsObject() ?? default))
        {
            if (item is null) continue;

            var docType = GetDocType(item, this._docTypeAliasValue);
            if (docType == null) continue;

            GetPropertiesMethod(item, docType);
        }

        jsonValue.TrySerializeJsonNode(out var result);
        return result;
    }

    public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
    {
        var stringValue = GetValueAs<string>(value);
        if (stringValue == null || stringValue.TryConvertToJsonNode(out var json) is false || json is null) return [];

        if (json?.GetValueKind() != JsonValueKind.Array) return [];

        var jsonArray = json.AsArray();
        if (jsonArray.Count == 0) return [];

        var dependencies = new List<uSyncDependency>();

        foreach (var item in jsonArray.Select(x => x?.AsObject() ?? default))
        {
            if (item is null) continue;

            if (item.TryGetPropertyValue(this._docTypeAliasValue, out var propertyNode) is false || propertyNode == null)
                continue;

            var docTypeAlias = propertyNode.GetValue<string>();
            var docType = GetDocType(docTypeAlias);
            if (docType == null) continue;

            if (flags.HasFlag(DependencyFlags.IncludeDependencies))
            {
                var docTypeDep = CreateDocTypeDependency(docTypeAlias, flags);
                if (docTypeDep != null)
                    dependencies.Add(docTypeDep);
            }

            dependencies.AddRange(GetPropertyDependencies(item, docType, flags));
        }

        return dependencies;
    }
}

