using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Services;

using uSync8.Core;
using uSync8.Core.Dependency;

namespace uSync8.ContentEdition.Mapping.Mappers
{
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
    public class GridMapper : SyncValueMapperBase, ISyncMapper
    {
        public GridMapper(IEntityService entityService) : base(entityService)
        { }

        public override string Name => "Grid Mapper";

        public override string[] Editors => new string[] {
            Constants.PropertyEditors.Aliases.Grid
        };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            if (value == null) return Enumerable.Empty<uSyncDependency>();

            var stringValue = value.ToString();
            if (string.IsNullOrWhiteSpace(stringValue)) return Enumerable.Empty<uSyncDependency>();

            return GetGridDependencies(stringValue, ProcessControl, flags);
        }

        public override string GetImportValue(string value, string editorAlias)
        {
            var gridContent = this.GetValueAs<string>(value);
            if (string.IsNullOrWhiteSpace(gridContent)) return value;
            return ProcessGridValues(gridContent, ProcessImport);
        }

        public override string GetExportValue(object value, string editorAlias)
        {
            var gridContent = this.GetValueAs<string>(value);
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
        private string ProcessGridValues(string gridContent, Func<IEnumerable<ISyncMapper>, string, object, string> callback)
        {
            var grid = JsonConvert.DeserializeObject<JObject>(gridContent);
            if (grid == null) return gridContent;

            var sections = GetArray(grid, "sections");
            foreach (var section in sections.Cast<JObject>())
            {
                var rows = GetArray(section, "rows");
                foreach (var row in rows.Cast<JObject>())
                {
                    var areas = GetArray(row, "areas");
                    foreach (var area in areas.Cast<JObject>())
                    {
                        var controls = GetArray(area, "controls");
                        foreach (var control in controls.Cast<JObject>())
                        {
                            var editor = control.Value<JObject>("editor");
                            var value = control.Value<Object>("value");
                            var (alias, mappers) = FindMappers(editor);

                            if (mappers != null && mappers.Any())
                            {
                                var result = callback(mappers, alias, value);
                                if (result != string.Empty)
                                {
                                    control["value"] = result.GetJsonTokenValue();
                                }
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(grid, Formatting.Indented);
        }

        private string ProcessImport(IEnumerable<ISyncMapper> mappers, string editorAlias, object value)
        {
            var mappedValue = value.ToString();
            foreach (var mapper in mappers)
            {
                mappedValue = mapper.GetImportValue(mappedValue, editorAlias);
            }
            return mappedValue;
        }

        private string ProcessExport(IEnumerable<ISyncMapper> mappers, string editorAlias, object value)
        {
            var mappedValue = value.ToString();
            foreach (var mapper in mappers)
            {
                mappedValue = mapper.GetExportValue(mappedValue, editorAlias);
            }
            return mappedValue;
        }

        #region Dependency Checking 

        private IEnumerable<uSyncDependency> ProcessControl(JObject control, DependencyFlags flags)
        {
            var editor = control.Value<JObject>("editor");
            var value = control.Value<object>("value");

            if (value == null) return Enumerable.Empty<uSyncDependency>();

            var (alias, mappers) = FindMappers(editor);
            if (mappers == null || !mappers.Any()) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (var mapper in mappers)
            {
                dependencies.AddRange(mapper.GetDependencies(value, alias, flags));
            }
            return dependencies;
        }

        private IEnumerable<uSyncDependency> GetGridDependencies(string gridContent, Func<JObject, DependencyFlags, IEnumerable<uSyncDependency>> callback, DependencyFlags flags)
        {
            var grid = JsonConvert.DeserializeObject<JObject>(gridContent);
            if (grid == null) return Enumerable.Empty<uSyncDependency>();

            var items = new List<uSyncDependency>();

            var sections = GetArray(grid, "sections");
            foreach (var section in sections.Cast<JObject>())
            {
                var rows = GetArray(section, "rows");
                foreach (var row in rows.Cast<JObject>())
                {
                    var rowStyles = GetObject(row, "styles");
                    if (rowStyles != null) items.AddRange(GetStyleDependencies(rowStyles));

                    var areas = GetArray(row, "areas");
                    foreach (var area in areas.Cast<JObject>())
                    {
                        var areaStyles = GetObject(area, "styles");
                        if (areaStyles != null) items.AddRange(GetStyleDependencies(areaStyles));

                        var controls = GetArray(area, "controls");
                        foreach (var control in controls.Cast<JObject>())
                        {
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
        ///  Attempt to extact any media values out of the style sheet entries in the grid config.
        /// </summary>
        private IEnumerable<uSyncDependency> GetStyleDependencies(JObject style)
        {
            if (style == null) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (var value in style.ToObject<Dictionary<string, string>>())
            {
                // style property contains a url value.
                if (value.Value.InvariantContains("url")) { 
                    dependencies.AddRange(ProcessStyleMedia(value.Value));
                }
            }

            return dependencies;
        }

        private Regex UrlRegEx = new Regex(@"(?:url\s*[(]*\s*)(.+)(?:[)])", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        ///  Process a URL() value and get the media dependency for the inner value (if there is one).
        /// </summary>
        /// <param name="urlValue"></param>
        /// <returns></returns>
        private IEnumerable<uSyncDependency> ProcessStyleMedia(string urlValue)
        {
            foreach(Match match in UrlRegEx.Matches(urlValue))
            {
                if (match.Groups.Count > 1)
                {
                    var mediaPath = match.Groups[1].Value.Trim(new char[] { '\'', '\"' });
                    var item = Current.Services.MediaService.GetMediaByPath(mediaPath);
                    if (item != null)
                    {
                        yield return CreateDependency(item.GetUdi(), DependencyFlags.IncludeMedia);
                    }
                }
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
        private (string alias, IEnumerable<ISyncMapper> mapper) FindMappers(JObject editor)
        {
            var alias = $"{Constants.PropertyEditors.Aliases.Grid}.{editor.Value<string>("alias")}";

            var mappers = SyncValueMapperFactory.GetMappers(alias);
            if (mappers.Any()) return (alias, mappers);

            // look based on the view 
            var viewAlias = editor.Value<string>("view");
            if (viewAlias == null) return (alias, Enumerable.Empty<ISyncMapper>());

            viewAlias = viewAlias.ToLower().TrimEnd(".html");

            if (viewAlias.IndexOf('/') != -1)
                viewAlias = viewAlias.Substring(viewAlias.LastIndexOf('/') + 1);

            alias = $"{Constants.PropertyEditors.Aliases.Grid}.{viewAlias}";
            return (alias, SyncValueMapperFactory.GetMappers(alias));
        }

        private JArray GetArray(JObject obj, string propertyName)
        {
            if (obj.TryGetValue(propertyName, out JToken token))
            {
                if (token is JArray array)
                    return array;
            }

            return new JArray();
        }

        private JObject GetObject(JObject obj, string propertyName)
        {
            if (obj.TryGetValue(propertyName, out JToken token))
            {
                if (token is JObject value)
                    return value;
            }

            return null;               

        }
    }
}
