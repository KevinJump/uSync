using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Mapping;

namespace uSync8.Community.Contrib.Mappers;

/// <summary>
///  value/dependency mapper for DocTypeGridEditor.
/// </summary>
/// <remarks>
///  <para>
///   In general for Umbraco 8 we don't need Value mappers, because
///   everything is Guid based
///  </para>
///  <para>
///   More relevant are the dependency finding functions, as these
///   will help uSync.Publisher / Exporter find out what linked
///   media/content and doctypes are needed to render your DTGE 
///   in another site. 
///  </para>
///  <para>
///   this mapper is more complicated than most need to be because
///   DTGE stores other content types within it, so we have to loop
///   into them and call the mappers for all the properties contained
///   within. Most of the time for simple mappers you don't need to 
///   do this. 
///  </para>
/// </remarks>
public class DocTypeGridMapper : SyncNestedValueMapperBase, ISyncMapper
{
    private readonly string docTypeAliasValue = "dtgeContentTypeAlias";

    private readonly ILogger<DocTypeGridMapper> _logger;

    public DocTypeGridMapper(IEntityService entityService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        ILogger<DocTypeGridMapper> logger)
        : base(entityService, mapperCollection, contentTypeService, dataTypeService)
    {
        this._logger = logger;
    }

    public override string Name => "DocType Grid Mapper";

    public override string[] Editors => [ "Umbraco.Grid.docType", "Umbraco.Grid.doctypegrideditor" ];

    /// <summary>
    ///  Get any formatted export values. 
    /// </summary>
    /// <remarks>
    ///  for 99% of properties you don't need to go in and get the 
    ///  potential internal values, but we do this on export because
    ///  we want to ensure we trigger formatting of Umbraco.DateTime values
    /// </remarks>
    public override string GetExportValue(object value, string editorAlias)
    {
        if (value is null) return string.Empty;

        var jsonValue = GetJsonValue(value);
        if (jsonValue == null) return value.ToString() ?? string.Empty;

        var docType = GetDocType(jsonValue, this.docTypeAliasValue);
        if (docType == null) return value.ToString() ?? string.Empty;

        // JArray of values 
        var docValue = jsonValue.GetPropertyAsObject("value");
        if (docValue == null) return value.ToString() ?? string.Empty;

        // the docTypeGrid editor wants the values in "real" json
        // as opposed to quite a few of these properties that 
        // have it in 'escaped' json. so slightly different 
        // then a nested content, but not by much.
        GetExportJsonValues(docValue, docType);

        return jsonValue.SerializeJsonString(true);
    }

    private JsonObject GetExportJsonValues(JsonObject item, IContentType docType)
    {
        foreach (var property in docType.CompositionPropertyTypes)
        {
            if (item.ContainsKey(property.Alias))
            {
                var value = item[property.Alias];
                if (value != null)
                {
                    var mappedVal = mapperCollection.Value.GetExportValue(value, property.PropertyEditorAlias).ToString();
                    item[property.Alias] = mappedVal.ConvertToJsonNode()?.ExpandAllJsonInToken();
                }
            }
        }

        return item;
    }


    public override string? GetImportValue(string value, string editorAlias)
    {
        try
        {
            if (value == null) return string.Empty;

            var jsonValue = GetJsonValue(value);
            if (jsonValue == null) return value.ToString();

            var docType = GetDocType(jsonValue, this.docTypeAliasValue);
            if (docType == null) return value.ToString();

            // JArray of values 
            var docValue = jsonValue.GetPropertyAsObject("value");
            if (docValue == null) return value.ToString();

            // the docTypeGridEditor wants the values in "real" json
            // as opposed to quite a few of these properties that 
            // have it in 'escaped' json. so slightly different 
            // then a nested content, but not by much.
            GetImportJsonValue(docValue, docType);

            return jsonValue.ExpandAllJsonInToken().SerializeJsonString(true);
        }
        catch (Exception ex)
        {
            // we want to be quite non-destructive on an import, 
            _logger.LogWarning(ex, "Failed to sanitize the import value for property (turn on debugging for full property value)");
            _logger.LogDebug("Failed DocTypeValue: {value}", value ?? String.Empty);

            return value;
        }
    }

    private JsonObject GetImportJsonValue(JsonObject item, IContentType docType)
    {
        foreach (var property in docType.CompositionPropertyTypes)
        {
            if (item.ContainsKey(property.Alias))
            {
                var value = item[property.Alias];
                if (value != null)
                {
                    var mappedVal = mapperCollection.Value.GetImportValue(value.ToString(), property.PropertyEditorAlias)?.ToString();
                    if (mappedVal is not null)
                    {
                        item[property.Alias] = mappedVal.ConvertToJsonNode()?.ExpandAllJsonInToken();
                    }

                }
            }
        }

        return item;
    }

    public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
    {
        var jsonValue = GetJsonValue(value);
        if (value == null || jsonValue == null) return [];

        var docValue = jsonValue.GetPropertyAsObject("value");
        var docTypeAlias = jsonValue.GetPropertyAsString(this.docTypeAliasValue);
        if (docValue == null || docTypeAlias == null) return [];

        var docType = GetDocType(docTypeAlias);
        if (docType == null) return [];

        List<uSyncDependency> dependencies = [];

        if (flags.HasFlag(DependencyFlags.IncludeDependencies))
        {
            // get the docType as a dependency. 
            // you only need to get the primary doctype, a subsequent check
            // will get the full dependency tree for this doctype if it
            // is needed. 
            var docDependency = CreateDocTypeDependency(docTypeAlias, flags);
            if (docDependency != null)
                dependencies.Add(docDependency);
        }

        // let the base class go through the PropertyTypes 
        // and call the mappers for each value, this gets us 
        // any internal dependencies (like media, etc) 
        // from within the content. 
        dependencies.AddRange(GetPropertyDependencies(docValue, docType, flags));

        return dependencies;
    }

}
