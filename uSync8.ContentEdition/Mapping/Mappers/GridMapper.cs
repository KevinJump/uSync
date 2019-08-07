using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
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
        {
        }

        public override string Name => "Grid Mapper";

        public override string[] Editors => new string[] { "Umbraco.Grid" };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var stringValue = value.ToString();
            if (string.IsNullOrWhiteSpace(stringValue)) return Enumerable.Empty<uSyncDependency>();

            return ProcessGrid<uSyncDependency>(stringValue, ProcessControl, flags);
        }

        private IEnumerable<uSyncDependency> ProcessControl(JObject control, DependencyFlags flags)
        {
            var editor = control.Value<JObject>("editor");
            var value = control.Value<object>("value");

            var (alias, mapper) = FindMapper(editor);
            if (mapper == null) return Enumerable.Empty<uSyncDependency>();
            return mapper.GetDependencies(value, alias, flags);
        }

        private (string alias, ISyncMapper mapper) FindMapper(JObject editor)
        {
            var alias = $"Umbraco.Grid.{editor.Value<string>("alias")}";

            var mapper = SyncValueMapperFactory.GetMapper(alias);
            if (mapper != null) return (alias, mapper);

            // look based on the view 
            var viewAlias = editor.Value<string>("view");
            if (viewAlias == null) return (alias, null);

            if (viewAlias.IndexOf('.') != -1)
                viewAlias = viewAlias.Substring(viewAlias.LastIndexOf('.'));

            alias = $"Umbraco.Grid.{viewAlias}";
            return (alias, SyncValueMapperFactory.GetMapper(alias));
        }


        private IEnumerable<TObject> ProcessGrid<TObject>(string gridContent, Func<JObject, DependencyFlags, IEnumerable<TObject>> callback, DependencyFlags flags)
        {
            var grid = JsonConvert.DeserializeObject<JObject>(gridContent);
            if (grid == null) return Enumerable.Empty<TObject>();

            var items = new List<TObject>();

            var sections = GetArray(grid, "sections");
            foreach(var section in sections.Cast<JObject>())
            {
                var rows = GetArray(section, "rows");
                foreach(var row in rows.Cast<JObject>())
                {
                    var areas = GetArray(row, "areas");
                    foreach(var area in areas.Cast<JObject>())
                    {
                        var controls = GetArray(area, "controls");
                        foreach(var control in controls.Cast<JObject>())
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
