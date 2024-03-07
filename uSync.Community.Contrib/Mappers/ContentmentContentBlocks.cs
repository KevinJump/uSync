using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

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

    public override string[] Editors => new string[]
    {
        "Umbraco.Community.Contentment.ContentBlocks"
    };

    protected override string ProcessValues(JsonObject jsonValue, string editorAlias, Func<JsonObject, IContentType, JsonObject> GetPropertiesMethod)
    {
        var elements = jsonValue.AsArray();

        foreach (var item in elements.AsListOfJsonObjects())
        {
            var itemValue = item?.GetPropertyAsObject("value");
            if (itemValue is null) continue;

            var doctype = GetDocTypeByKey(item, "elementType");
            if (doctype is null) continue;

            GetImportProperties(itemValue, doctype);
        }

        return elements.SerializeJsonNode(false);

    }

    public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
    {
        var stringValue = GetValueAs<string>(value);

        if (stringValue.TryParseToJsonArray(out JsonArray? elements) is false || elements is null)
            return Enumerable.Empty<uSyncDependency>();

        if (elements == null || !elements.Any())
            return Enumerable.Empty<uSyncDependency>();

        var dependencies = new List<uSyncDependency>();

        foreach (var item in elements.AsListOfJsonObjects())
        {
            if (item is null) continue;

            var itemValue = item.GetPropertyAsObject("value");
            if (itemValue == null) continue;

            var doctype = GetDocTypeByKey(item, "elementType");
            if (doctype == null) continue;

            if (flags.HasFlag(DependencyFlags.IncludeDependencies))
            {
                var doctypeDependency = CreateDocTypeDependency(doctype.Alias, flags);
                if (doctypeDependency != null) dependencies.Add(doctypeDependency);
            }

            dependencies.AddRange(GetPropertyDependencies(itemValue, doctype, flags));
        }

        return dependencies;
    }
}
