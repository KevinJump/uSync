using Microsoft.Extensions.Logging;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Serialization;
using uSync.Core.Versions;
using uSync.Core.Versions._18._0;

namespace uSync.Core.Mapping;

/// <summary>
///  mapper for the tinyMCE editor 
/// </summary>
/// <remarks>
/// Can be tricky because it contains embedded links 
/// 
/// "<p>Content Updated with a <a data-udi=\"umb://document/469b6e232ae04dcdb4a26e857f75e1fb\" href=\"/{localLink:umb://document/469b6e232ae04dcdb4a26e857f75e1fb}\" title=\"ContentTemplate\">link</a></p>" 
/// </remarks>
public partial class RTEMapper : SyncValueMapperBase, ISyncMapper, ISyncPropertyMapper
{
    private readonly Lazy<SyncValueMapperCollection> _mapperCollection;
    private readonly SyncLocalLinkProcessor _localLinkProcessor;
    private readonly ILogger<RTEMapper> _logger;
    private readonly IIdKeyMap _idKeyMap;
    private readonly ISyncImageUpdateHelper _syncImageUpdater;

    public RTEMapper(
        IEntityService entityService,
        Lazy<SyncValueMapperCollection> mappers,
        SyncLocalLinkProcessor localLinkProcessor,
        ILogger<RTEMapper> logger,
        IIdKeyMap idKeyMap,
        ISyncImageUpdateHelper syncImageUpdateHelper)
        : base(entityService)
    {
        _mapperCollection = mappers;
        _localLinkProcessor = localLinkProcessor;
        _logger = logger;
        _idKeyMap = idKeyMap;
        _syncImageUpdater = syncImageUpdateHelper;
    }

    // would preferer the link regex - less likely to get rouge ones 
    // private string linkRegEx = "((?&lt;=localLink:)([0-9]+)|(?&lt;=data-id=&quot;)([0-9]+))";
    private Regex UdiRegEx = UdiRegExPattern();
    private Regex MacroRegEx = MacroRegExPattern();
    private Regex LinkRegEx = LinkRegExPattern();

    public override string Name => "TinyMCE RTE Mapper";

    public override string[] Editors => [
        "Umbraco.TinyMCE",
        Constants.PropertyEditors.Aliases.RichText,
        $"{Constants.PropertyEditors.Aliases.Grid}.rte"
    ];

    public override Task<string?> GetImportValueAsync(string value, IPropertyType propertyType, SyncSerializerOptions options)
    {
        var mapHmacValues = options.GetSetting<bool>("MapHMACValues", false);

        if (value.TryParseToJsonObject(out var jsonObject) is false || jsonObject is null)
            return base.GetImportValueAsync(value, propertyType, options);

        if (jsonObject.TryGetPropertyValue("markup", out var markupNode) is false || markupNode is null)
            return base.GetImportValueAsync(value, propertyType, options);

        // migrate the markup content if needed
        var migratedMarkup = markupNode.ToString();
        if (migratedMarkup is null)
            return base.GetImportValueAsync(value, propertyType, options);

        // This migration is going to be removed from Umbraco at some point, (which will break native migrations from v13 -> v18+)
        // we need to replace the functionality with our own if we want usync to migrate correctly across this version bar.
        var markup = _localLinkProcessor.ProcessStringValue(migratedMarkup);

        // check if we need to update at hmac values inside the site. 
        if (mapHmacValues is true) 
            markup = _syncImageUpdater.UpdateImageUrlValues(markup);

        jsonObject["markup"] = markup;

        return Task.FromResult<string?>(jsonObject.SerializeJsonString());
    }

    public override async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        // value null check. 
        if (value == null) return [];

        var stringValue = value.ToString();
        if (string.IsNullOrWhiteSpace(stringValue)) return [];

        if (stringValue.TryParseToJsonNode(out var jsonNode) && jsonNode is not null)
        {
            // if its json, it contains the new blocks way of sending shizzel. 
            return await GetBlockDependenciesAsync(jsonNode.AsObject(), editorAlias, flags);
        }

        return await GetSimpleDependenciesAsync(stringValue, editorAlias, flags);
    }

    private async Task<List<uSyncDependency>> GetBlockDependenciesAsync(JsonObject jObject, string editorAlias, DependencyFlags flags)
    {
        var dependencies = new List<uSyncDependency>();

        if (jObject.TryGetPropertyValue("markup", out var markupNode) && markupNode is not null)
        {
            dependencies.AddRange(await GetSimpleDependenciesAsync(markupNode.ToString(), editorAlias, flags));
        }

        if (jObject.TryGetPropertyValue("blocks", out var blocks) && blocks is not null)
        {
            dependencies.AddRange(await _mapperCollection.Value.GetDependenciesAsync(blocks, Constants.PropertyEditors.Aliases.BlockList, flags));
        }

        return dependencies;
    }

    private async Task<IEnumerable<uSyncDependency>> GetSimpleDependenciesAsync(string stringValue, string editorAlias, DependencyFlags flags)
    {
        if (string.IsNullOrWhiteSpace(stringValue)) return [];

        var dependencies = new List<uSyncDependency>();

        dependencies.AddRange(FindUdiDependencies(UdiRegEx, stringValue, flags));

        // FindGuidDependencies can result in a db lookup. So we only call it if we know we want to include the dependencies.
        if (flags.HasFlag(DependencyFlags.IncludeLinked) || flags.HasFlag(DependencyFlags.IncludeMedia))
            dependencies.AddRange(FindGuidDependencies(LinkRegEx, stringValue, flags));

        // macros should never really be here, so this never gets called - we will remove this functionality in Umbraco 18
        if (MacroRegEx.IsMatch(stringValue))
            dependencies.AddRange(await GetMacroDependencies(stringValue, editorAlias, flags));

        return dependencies.Distinct();
    }

    /// <summary>
    ///  process macros inside any RTE Content, this actually shouldn't be possible v14+, so we will remove this functionality in Umbraco 18.
    /// </summary>
    private async Task<IEnumerable<uSyncDependency>> GetMacroDependencies(string stringValue, string editorAlias, DependencyFlags flags)
    {
        var dependencies = new List<uSyncDependency>();

        var mappers = _mapperCollection.Value.GetSyncMappers(editorAlias + ".macro");
        if (mappers.Any())
        {
            foreach (var mapper in MacroRegEx.Matches(stringValue).SelectMany(macro => mappers))
            {
                dependencies.AddRange(await mapper.GetDependenciesAsync(stringValue, editorAlias + ".macro", flags));
            }
        }

        return dependencies;
    }

    /// <summary>
    ///  find any umb://.. dependencies inside the content string
    /// </summary>
    private IEnumerable<uSyncDependency> FindUdiDependencies(Regex regex, string stringValue, DependencyFlags flags)
    {
        foreach (Match m in regex.Matches(stringValue))
        {
            if (UdiParser.TryParse(m.Value, out GuidUdi? udi) && udi is not null)
            {
                // only include when we are linking everything or including media, otherwise it can basically spider the site.
                if (flags.HasFlag(DependencyFlags.IncludeLinked) == false && udi.EntityType != Constants.UdiEntityType.Media)
                    continue;

                var dependency = CreateDependency(udi, flags);
                if (dependency is not null)
                    yield return dependency;
            }
        }
    }

    /// <summary>
    ///  find any "locallink:" dependencies in the content. 
    /// </summary>
    private IEnumerable<uSyncDependency> FindGuidDependencies(Regex regex, string stringValue, DependencyFlags flags)
    {
        var includeLinkedItems = flags.HasFlag(DependencyFlags.IncludeLinked);

        foreach (Match m in regex.Matches(stringValue))
        {
            var guidString = m.Value.Trim('{', '}').Replace("localLink:", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            if (Guid.TryParse(guidString, out Guid guid) is false) continue;

            var guidUdi = TryGetGuidUdiFromGuid(guid, 
                [
                    UmbracoObjectTypes.Media, 
                    includeLinkedItems ? UmbracoObjectTypes.Document : UmbracoObjectTypes.Unknown
                ]);

            if (guidUdi is null) continue;

            // we only include when its media or we are including all linked items, otherwise it can basically
            // spider the site. 
            if (includeLinkedItems|| guidUdi.EntityType == Constants.UdiEntityType.Media)
            {
                var dependency = CreateDependency(guidUdi, flags);
                if (dependency is null) continue;
                yield return dependency;
            }
        }
    }

    private GuidUdi? TryGetGuidUdiFromGuid(Guid guid, UmbracoObjectTypes[] types)
    {
        try
        {
            foreach (var type in types)
            {
                if (type == UmbracoObjectTypes.Unknown)
                    continue;

                var attempt = _idKeyMap.GetIdForKey(guid, type);
                if (attempt.Success)
                {
                    var entityAlias = UdiEntityTypeHelper.FromUmbracoObjectType(type);
                    return new GuidUdi(entityAlias, guid);
                }
            }

            return null;
        }
        catch
        {
            _logger.LogWarning("Failed to resolve GUID [{guid}] to an entity. This may indicate a broken link in the RTE content.", guid);
            return null;
        }   
    }

    [GeneratedRegex(@"(umb:[/\\]+[a-zA-Z-]+[/\\][a-zA-Z0-9-]+)",
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "en-GB")]
    private static partial Regex UdiRegExPattern();

    [GeneratedRegex(@"\{localLink:[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\}",
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "en-GB")]
    private static partial Regex LinkRegExPattern();

    [GeneratedRegex("<\\?UMBRACO_MACRO[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "en-GB")]
    private static partial Regex MacroRegExPattern();
}
