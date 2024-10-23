using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

/// <summary>
///  mapper for the tinyMCE editor 
/// </summary>
/// <remarks>
/// Can be tricky because it contains embedded links 
/// 
/// "<p>Content Updated with a <a data-udi=\"umb://document/469b6e232ae04dcdb4a26e857f75e1fb\" href=\"/{localLink:umb://document/469b6e232ae04dcdb4a26e857f75e1fb}\" title=\"ContentTemplate\">link</a></p>" 
/// </remarks>
public class RTEMapper : SyncValueMapperBase, ISyncMapper
{
    private readonly Lazy<SyncValueMapperCollection> _mapperCollection;

    public RTEMapper(
        IEntityService entityService,
        Lazy<SyncValueMapperCollection> mappers)
        : base(entityService)
    {
        _mapperCollection = mappers;
    }

    // would preferer the link regex - less likely to get rouge ones 
    // private string linkRegEx = "((?&lt;=localLink:)([0-9]+)|(?&lt;=data-id=&quot;)([0-9]+))";
    private Regex UdiRegEx = new Regex(@"(umb:[/\\]+[a-zA-Z-]+[/\\][a-zA-Z0-9-]+)",
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

    private Regex MacroRegEx = new Regex("<\\?UMBRACO_MACRO[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

    public override string Name => "TinyMCE RTE Mapper";

    public override string[] Editors => new string[] {
        "Umbraco.TinyMCE",
        Constants.PropertyEditors.Aliases.RichText,
        $"{Constants.PropertyEditors.Aliases.Grid}.rte"
    };

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

        foreach (Match m in UdiRegEx.Matches(stringValue))
        {
            if (UdiParser.TryParse(m.Value, out GuidUdi? udi) && udi is not null)
            {
                if (!dependencies.Any(x => x.Udi == udi))
                    dependencies.AddNotNull(CreateDependency(udi, flags));
            }
        }

        if (MacroRegEx.IsMatch(stringValue))
        {
            var mappers = _mapperCollection.Value.GetSyncMappers(editorAlias + ".macro");
            if (mappers.Any())
            {
                foreach (var mapper in MacroRegEx.Matches(stringValue).SelectMany(macro => mappers))
                {
                    dependencies.AddRange(await mapper.GetDependenciesAsync(stringValue, editorAlias + ".macro", flags));
                }
            }
        }

        return dependencies.Distinct();
    }
}
