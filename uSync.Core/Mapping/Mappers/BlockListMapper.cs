using System.Text.Json;
using System.Text.Json.Nodes;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

public class BlockListMapper : SyncNestedJsonValueMapperBase, ISyncMapper
{
    private readonly string _docTypeKeyAlias = "contentTypeKey"; //  BlockEditorPropertyEditor.ContentTypeKeyPropertyKey;
    private readonly string[] _contentBlocks = ["contentData", "settingsData"];

    public BlockListMapper(IEntityService entityService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService)
        : base(entityService, mapperCollection, contentTypeService, dataTypeService)
    { }

    public override string Name => "Block List/Grid Mapper";

    public override string[] Editors => [
        Constants.PropertyEditors.Aliases.BlockList,
        Constants.PropertyEditors.Aliases.BlockGrid];

    protected override JsonNode? GetImportProperty(object value)
    {
        if (value == null) return null;
        var stringValue = value.GetValueAs<string>();

        if (stringValue == null) return default;

        if (stringValue.TryParseToJsonNode(out var jsonNode) is false || jsonNode is null)
            return stringValue.ConvertToJsonNode() ?? default; ;

        // we have to get the json, the serialize the json,
        // this is to make sure we don't serialize any formatting
        // (like indented formatting). because that would 
        // register changes that are not there.
        return jsonNode.SerializeJsonNode(false).ToJsonObject();
    }


    protected override JsonNode? GetExportProperty(string value)
        => value.TryConvertToJsonNode(out var node) ? node : default;

    protected override string ProcessValues(JsonObject jsonValue, string editorAlias,
        Func<JsonObject, IContentType, JsonObject> GetPropertiesMethod)
    {
        if (jsonValue.GetValueKind() == JsonValueKind.Object)
        {
            var jsonObject = jsonValue.AsObject();

            foreach (var block in _contentBlocks)
            {
                if (jsonObject.ContainsKey(block))
                {
                    var contentData = jsonObject.GetPropertyAsArray(block);
                    if (contentData == null) continue;

                    foreach (var item in contentData.AsListOfJsonObjects())
                    {
                        if (item is null) continue;

                        var doctype = GetDocTypeByKey(item, _docTypeKeyAlias);
                        if (doctype == null) continue;

                        GetPropertiesMethod(item, doctype);
                    }
                }
            }

            return jsonObject.SerializeJsonNode();
        }

        return jsonValue.SerializeJsonNode();
    }


    public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
    {
        var jsonValue = GetJsonValue(value);
        if (jsonValue == null) return [];


        var dependencies = new List<uSyncDependency>();

        // format of block data.
        // { "layout" : {}, "contentData": {}, "settingsData": {} }

        foreach (var block in _contentBlocks)
        {
            // contentData is the thing we need to inspect. 
            if (jsonValue.ContainsKey(block))
            {
                var contentData = jsonValue.GetPropertyAsArray(block);
                if (contentData != null)
                {
                    foreach (var contentItem in contentData.AsListOfJsonObjects())
                    {
                        if (contentItem is null) continue;

                        var contentType = GetDocTypeByKey(contentItem, this._docTypeKeyAlias);
                        if (contentType != null)
                        {
                            dependencies.AddNotNull(CreateDocTypeDependency(contentType, flags));
                            dependencies.AddRange(this.GetPropertyDependencies(contentItem, contentType, flags));
                        }
                    }
                }
            }
        }

        return dependencies;
    }
}
