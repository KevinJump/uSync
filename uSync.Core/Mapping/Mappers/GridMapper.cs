using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

using uSync.Core.Dependency;
using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

/// <summary>
///  mapper for the grid
/// </summary>
/// <remarks>
///  still no easy way to do this, other than traverse the JSON. 
///  
///  Here we have a processGrid function
///    it passes the each bit of Control JSON to a callback 
///    the callback then does what it needs to do. 
///    
///  in the grid, we append Umbraco.Grid to control aliases, 
///  so the RTE becomes Umbraco.Grid.RTE 
/// </remarks>
public partial class GridMapper : SyncValueMapperBase, ISyncMapper
{
    protected readonly IMediaService mediaService;
    protected readonly Lazy<SyncValueMapperCollection> mapperCollection;
    public GridMapper(IEntityService entityService,
        Lazy<SyncValueMapperCollection> mappers,
        IMediaService mediaService) : base(entityService)
    {
        this.mapperCollection = mappers;
        this.mediaService = mediaService;
    }

    public override string Name => "Grid Mapper";

    public override string[] Editors => [Constants.PropertyEditors.Aliases.Grid];

    public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
    {
        if (value is null) return [];

        var stringValue = value.ToString();
        if (string.IsNullOrWhiteSpace(stringValue)) return [];

        return GetGridDependencies(stringValue, ProcessControl, flags);
    }

    public override string? GetImportValue(string value, string editorAlias)
    {
        var gridContent = GetValueAs<string>(value);
        if (string.IsNullOrWhiteSpace(gridContent)) return value;
        return ProcessGridValues(gridContent, ProcessImport);
    }

    public override string? GetExportValue(object value, string editorAlias)
    {
        var gridContent = GetValueAs<string>(value);
        if (string.IsNullOrWhiteSpace(gridContent)) return gridContent;
        return ProcessGridValues(gridContent, ProcessExport);
    }

    /// <summary>
    ///  work through the grid, and call the passed callback when you 
    ///  get to a control value. 
    /// </summary>
    /// <remarks>
    ///  Passing the callback lets us have the grid traversal code only
    ///  once for both import and exporting.
    /// </remarks>       
    private string ProcessGridValues(string gridContent, Func<IEnumerable<ISyncMapper>, string, object, string?> callback)
    {
        if (gridContent.TryParseToJsonObject(out var grid) is false || grid == null)
            return gridContent;

        var sections = grid.GetPropertyAsArray("sections");
        foreach (var section in sections.AsListOfJsonObjects())
        {
            if (section is null) continue;

            var rows = section.GetPropertyAsArray("rows");
            foreach (var row in rows.AsListOfJsonObjects())
            {
                if (row is null) continue;

                var areas = row.GetPropertyAsArray("areas");
                foreach (var area in areas.AsListOfJsonObjects())
                {
                    if (area is null) continue;

                    var controls = area.GetPropertyAsArray("controls");
                    foreach (var control in controls.AsListOfJsonObjects())
                    {
                        if (control is null) continue;


                        var editor = control.GetPropertyAsObject("editor");
                        var value = control["value"].TryConvertTo<object>();

                        var (alias, mappers) = FindMappers(editor);

                        if (mappers != null && mappers.Any())
                        {
                            var result = callback(mappers, alias, value);
                            if (string.IsNullOrEmpty(result) is false)
                            {
                                control["value"] = result.ConvertStringToExpandedJson();
                            }
                        }
                    }
                }
            }
        }

        return grid.SerializeJsonNode();
    }

    private string? ProcessImport(IEnumerable<ISyncMapper> mappers, string editorAlias, object value)
    {
        var mappedValue = value.ToString();
        foreach (var mapper in mappers)
        {
            if (mappedValue == null) continue;
            mappedValue = mapper.GetImportValue(mappedValue, editorAlias);
        }
        return mappedValue;
    }

    private string? ProcessExport(IEnumerable<ISyncMapper> mappers, string editorAlias, object value)
    {
        var mappedValue = value.ToString();
        foreach (var mapper in mappers)
        {
            if (mappedValue == null) continue;
            mappedValue = mapper.GetExportValue(mappedValue, editorAlias);
        }
        return mappedValue;
    }

    #region Dependency Checking 

    private IEnumerable<uSyncDependency> ProcessControl(JsonObject control, DependencyFlags flags)
    {
        var editor = control.GetPropertyAsObject("editor");
        var value = control["value"];

        if (value is null) return [];

        var (alias, mappers) = FindMappers(editor);
        if (mappers == null || !mappers.Any()) return [];

        var dependencies = new List<uSyncDependency>();

        foreach (var mapper in mappers)
        {
            dependencies.AddRange(mapper.GetDependencies(value, alias, flags));
        }
        return dependencies;
    }

    private List<uSyncDependency> GetGridDependencies(string gridContent,
        Func<JsonObject, DependencyFlags, IEnumerable<uSyncDependency>> callback, DependencyFlags flags)
    {
        var items = new List<uSyncDependency>();

        if (gridContent.TryParseToJsonObject(out var grid) is false || grid == null)
            return items;

        var sections = grid.GetPropertyAsArray("sections");
        foreach (var section in sections.AsListOfJsonObjects())
        {
            if (section is null) continue;
            var rows = section.GetPropertyAsArray("rows");

            foreach (var row in rows.AsListOfJsonObjects())
            {
                if (row is null) continue;

                var rowStyles = row.GetPropertyAsObject("styles");
                if (rowStyles != null) items.AddRange(GetStyleDependencies(rowStyles));

                var areas = row.GetPropertyAsArray("areas");

                foreach (var area in areas.AsListOfJsonObjects())
                {
                    if (area is null) continue;

                    var areaStyles = area.GetPropertyAsObject("styles");
                    if (areaStyles != null) items.AddRange(GetStyleDependencies(areaStyles));

                    var controls = area.GetPropertyAsArray("controls");

                    foreach (var control in controls.AsListOfJsonObjects())
                    {
                        if (control is null) continue;

                        var result = callback(control, flags);
                        if (result != null)
                            items.AddRange(result);
                    }
                }
            }
        }

        return items;
    }

    /// <summary>
    ///  Attempt to extract any media values out of the style sheet entries in the grid config.
    /// </summary>
    private List<uSyncDependency> GetStyleDependencies(JsonObject style)
    {
        if (style == null) return [];

        var dependencies = new List<uSyncDependency>();

        try
        {
            foreach (var value in style)
            {
                if (value.Value is null) continue;

                var stringValue = value.Value.ToString();
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    // style property contains a url value.
                    if (stringValue.InvariantContains("url"))
                    {
                        dependencies.AddRange(ProcessStyleMedia(stringValue));
                    }
                }
            }
        }
        catch
        {
            // ideally we want to deal with this, but failure of a dependency check on a url in 
            // a grid style element shouldn't stop a full export. 
        }

        return dependencies;
    }

    private readonly Regex _urlRegEx = UrlRegularExpression();

    [GeneratedRegex(@"(?:url\s*[(]*\s*)(.+)(?:[)])", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace, "en-US")]
    private static partial Regex UrlRegularExpression();

    /// <summary>
    ///  Process a URL() value and get the media dependency for the inner value (if there is one).
    /// </summary>
    /// <param name="urlValue"></param>
    /// <returns></returns>
    private IEnumerable<uSyncDependency> ProcessStyleMedia(string urlValue)
    {
        foreach (Match match in _urlRegEx.Matches(urlValue).Cast<Match>())
        {
            if (match.Groups.Count <= 1) continue;

            var mediaPath = match.Groups[1].Value.Trim(['\'', '\"']);
            var item = mediaService.GetMediaByPath(mediaPath);
            if (item is null) continue;

            var dependency = CreateDependency(item.GetUdi(), DependencyFlags.IncludeMedia);
            if (dependency is not null)
                yield return dependency;
        }
    }

    #endregion

    /// <summary>
    ///  Get the Mapper for this control
    /// </summary>
    /// <remarks>
    ///  For grid elements we add the "alias" value to Umbraco.Grid. 
    ///  to get the editor alias, this for example makes the 
    ///  DocTypeGridEditor have an alias of Umbraco.Grid.docType 
    /// </remarks>
    /// <returns></returns>
    private (string alias, IEnumerable<ISyncMapper> mapper) FindMappers(JsonObject? editor)
    {
        if (editor is null || editor.ContainsKey("alias") is false || editor.ContainsKey("view") is false)
            throw new Exception("Grid editor is missing alias and view");


        var editorAlias = editor["alias"]?.GetValue<string>() ?? string.Empty;
        var viewAlias = editor["view"]?.GetValue<string>() ?? string.Empty;

        var alias = $"{Constants.PropertyEditors.Aliases.Grid}.{editorAlias}";

        var mappers = mapperCollection.Value.GetSyncMappers(alias);
        if (mappers.Any()) return (alias, mappers);

        // look based on the view 
        if (viewAlias == null) return (alias, Enumerable.Empty<ISyncMapper>());

        viewAlias = viewAlias.ToLower().TrimEnd(".html");

        if (viewAlias.Contains('/'))
            viewAlias = viewAlias.Substring(viewAlias.LastIndexOf('/') + 1);

        alias = $"{Constants.PropertyEditors.Aliases.Grid}.{viewAlias}";
        return (alias, mapperCollection.Value.GetSyncMappers(alias));
    }
}
