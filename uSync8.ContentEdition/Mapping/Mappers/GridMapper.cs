﻿using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NPoco.FluentMappings;

using Umbraco.Core;
using Umbraco.Core.Services;

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

            return GetGridDependencies<uSyncDependency>(stringValue, ProcessControl, flags);
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

                            if (mappers.Any())
                            {
                                var result = callback(mappers, alias, value);
                                if (result != string.Empty)
                                {
                                    if (result.DetectIsJson())
                                    {
                                        control["value"] = JToken.Parse(result);
                                    }
                                    else
                                    {
                                        control["value"] = result;
                                    }
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
            if (!mappers.Any()) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            foreach (var mapper in mappers)
            {
                dependencies.AddRange(mapper.GetDependencies(value, alias, flags));
            }
            return dependencies;
        }

        private IEnumerable<TObject> GetGridDependencies<TObject>(string gridContent, Func<JObject, DependencyFlags, IEnumerable<TObject>> callback, DependencyFlags flags)
        {
            var grid = JsonConvert.DeserializeObject<JObject>(gridContent);
            if (grid == null) return Enumerable.Empty<TObject>();

            var items = new List<TObject>();

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
                            var result = callback(control, flags);
                            if (result != null)
                                items.AddRange(result);
                        }
                    }
                }
            }

            return items;
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
            if (viewAlias == null) return (alias, null);

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
    }
}
