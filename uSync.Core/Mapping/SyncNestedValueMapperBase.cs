﻿using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

/// <summary>
///  a base class for properties that nest other content items inside them
/// </summary>
/// <remarks>
///  this base class can be used to kickstart a value mapper for anything
///  that stores other doctypes inside of its own values (NestedContent, DTGE)
/// </remarks>
public abstract class SyncNestedValueMapperBase : SyncValueMapperBase
{
    protected readonly IContentTypeService contentTypeService;
    protected readonly IDataTypeService dataTypeService;

    protected readonly Lazy<SyncValueMapperCollection> mapperCollection;

    public SyncNestedValueMapperBase(IEntityService entityService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService)
        : base(entityService)
    {
        this.mapperCollection = mapperCollection;

        this.contentTypeService = contentTypeService;
        this.dataTypeService = dataTypeService;
    }

    /// <summary>
    ///   Gets the import property representation as a JToken
    /// </summary>
    /// <remarks>
    ///  this usually is a bit of nested json, but sometimes 
    ///  some properties want it to be json serialized into a string.
    /// </remarks>
    protected virtual JsonNode? GetImportProperty(object value)
    {
        if (value.TryConvertToJsonNode(out var jsonNode))
            return jsonNode;

        return default;
    }

    protected virtual JsonNode? GetExportProperty(string value)
    {
        if (value.TryConvertToJsonNode(out var jsonNode))
            return jsonNode;

        return default;
    }

    /// <summary>
    ///  Get the import value for properties used in the this JObject
    /// </summary>
    protected JsonObject GetImportProperties(JsonObject item, IContentType docType)
    {
        foreach (var property in docType.CompositionPropertyTypes)
        {
            if (item.ContainsKey(property.Alias))
            {
                var value = item[property.Alias];
                if (value != null)
                {
                    var mappedVal = mapperCollection.Value.GetImportValue(GetStringValue(value), property.PropertyEditorAlias);
                    if (mappedVal != null)
                    {
                        item[property.Alias] = GetImportProperty(mappedVal);
                    }
                }
            }
        }

        return item;
    }

    /// <summary>
    ///  get the export value for the properties used in this JObject
    /// </summary>
    protected JsonObject GetExportProperties(JsonObject item, IContentType docType)
    {
        foreach (var property in docType.CompositionPropertyTypes)
        {
            if (item.ContainsKey(property.Alias))
            {
                var value = item[property.Alias];
                if (value != null)
                {
                    var mappedVal = mapperCollection.Value.GetExportValue(value, property.PropertyEditorAlias);
                    item[property.Alias] = GetExportProperty(mappedVal);
                }
            }
        }

        return item;
    }

    protected IEnumerable<uSyncDependency> GetPropertyDependencies(JsonObject value,
        IContentType docType, DependencyFlags flags)
    {
        var dependencies = new List<uSyncDependency>();

        foreach (var propertyType in docType.CompositionPropertyTypes)
        {
            var propertyValue = value[propertyType.Alias];
            if (propertyValue == null) continue;

            var dataType = dataTypeService.GetDataType(propertyType.DataTypeKey);
            if (dataType == null) continue;

            dependencies.AddRange(mapperCollection.Value.GetDependencies(propertyValue, dataType.EditorAlias, flags));
        }

        return dependencies;
    }

    /// <summary>
    ///  get all the dependencies for a series of properties
    /// </summary>
    /// <param name="properties">Key, Value pair, of editorAlias, value</param>
    protected IEnumerable<uSyncDependency> GetPropertyDependencies(
        IDictionary<string, object> properties, DependencyFlags flags)
    {

        if (!properties.Any())
            return Enumerable.Empty<uSyncDependency>();

        var dependencies = new List<uSyncDependency>();
        foreach (var property in properties)
        {
            dependencies.AddRange(mapperCollection.Value.GetDependencies(property.Value, property.Key, flags));
        }

        return dependencies;
    }


    /// <summary>
    ///  Gets the dependency item for a doctype. 
    /// </summary>
    protected uSyncDependency? CreateDocTypeDependency(string alias, DependencyFlags flags)
    {
        var item = GetDocType(alias);
        if (item != null)
        {
            return CreateDocTypeDependency(item, flags);
        }

        return null;
    }

    protected static uSyncDependency? CreateDocTypeDependency(IContentType item, DependencyFlags flags)
    {
        if (item != null)
        {
            _ = new uSyncDependency
            {
                Name = item.Name ?? "",
                Udi = item.GetUdi(),
                Order = DependencyOrders.ContentTypes,
                Flags = flags & ~DependencyFlags.IncludeAncestors,
                Level = item.Level
            };
        }

        return null;
    }


    protected static JsonObject? GetJsonValue(object value)
    {
        var stringValue = GetValueAs<string>(value);
        if (string.IsNullOrWhiteSpace(stringValue)) return null;

        if (stringValue.TryParseToJsonNode(out var jsonNode) && jsonNode is not null)
            return jsonNode.AsObject();

        return null;
    }

    protected IContentType? GetDocType(JsonObject json, string alias)
    {
        if (json.ContainsKey(alias))
        {
            var docTypeAlias = json.GetValueAsString(alias, string.Empty);
            if (string.IsNullOrWhiteSpace(docTypeAlias)) return default;
            return GetDocType(docTypeAlias);
        }

        return default;
    }

    protected IContentType? GetDocTypeByKey(JsonObject? json, string keyAlias)
    {
        if (json?.ContainsKey(keyAlias) is true)
        {
            var attempt = json[keyAlias].TryConvertTo<Guid>();
            if (attempt.Success)
            {
                return mapperCollection.Value?.EntityCache.GetContentType(attempt.Result)
                     ?? contentTypeService.Get(attempt.Result);
            }
        }

        return default;
    }

    protected IContentType? GetDocType(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return default;
        return mapperCollection.Value?.EntityCache.GetContentType(alias)
            ?? contentTypeService.Get(alias);
    }

    protected static string GetStringValue(JsonNode value)
    {
        // TODO: Date formatting (can it be done at the options level?)
        if (value.TrySerializeJsonNode(out var valueJson))
            return valueJson;

        // else ?
        return value.ToJsonString();
    }
}
