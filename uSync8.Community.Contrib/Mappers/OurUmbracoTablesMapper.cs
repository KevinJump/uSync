using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Services;
using Umbraco.Core;
using uSync8.ContentEdition.Mapping;
using uSync8.Core.Dependency;
using ImageProcessor.Processors;
using Newtonsoft.Json.Converters;
using uSync8.ContentEdition.Mapping.Mappers;

namespace uSync8.Community.Contrib.Mappers
{
    /// <summary>
    /// This is a mapper for the V8 only 'Table' property editor called Our.Umbraco.Tables
    /// https://github.com/rydigital/Our.Umbraco.Tables/tree/master
    /// The value is stored as JSON with 'Table Settings', 'Table Rows' and 'Table Columns' defining the structure of the table
    /// Content lives in the Cells property, consisting of a row / column coordinate and the a value which is 'just' the output of an RTE stored as a value property
    /// This RTE can contain Media Items or Links to Media Items, and it's these dependencies that are missed when moving this content between environments using the default mappers
    /// </summary>
    public class OurUmbracoTablesMapper : SyncValueMapperBase, ISyncMapper
    {
        public OurUmbracoTablesMapper(IEntityService entityService) : base(entityService)
        {
        }

        public override string Name => "OurUmbracoTables Mapper";

        public override string[] Editors => new string[] {
            "Our.Umbraco.Tables"
        };

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            var tableData = JsonConvert.DeserializeObject<TableData>(value.ToString());
            if (tableData == null || tableData.Cells == null || !tableData.Cells.Any())
            {
                return Enumerable.Empty<uSyncDependency>();
            }
            //think we 'just' need to call the RTE Mapper for each stored value to discover dependencies
            var rteMappers = SyncValueMapperFactory.GetMappers(Constants.PropertyEditors.Aliases.TinyMce);
            // now we need to loop through each cell and find any dependencies
            var dependencies = new List<uSyncDependency>();
            foreach (var cell in tableData.Cells)
            {
                foreach (var cellData in cell)
                {
                    // there maybe multiple mappers for the TinyMCE?
                    foreach (var rteMapper in rteMappers)
                    {
                        dependencies.AddRange(rteMapper.GetDependencies(cellData.Value, Constants.PropertyEditors.Aliases.TinyMce, flags));
                    }
                }
            }
            return dependencies;
        }

        // taken from the source: https://github.com/rydigital/Our.Umbraco.Tables/blob/master/src/Our.Umbraco.Tables/Models/TableData.cs
        internal class TableData
        {
            [JsonProperty("settings")]
            public StyleData Settings { get; set; } = new StyleData();

            [JsonProperty("rows")]
            public IEnumerable<StyleData> Rows { get; set; } = new List<StyleData>();

            [JsonProperty("columns")]
            public IEnumerable<StyleData> Columns { get; set; } = new List<StyleData>();

            [JsonProperty("cells")]
            public IEnumerable<IEnumerable<CellData>> Cells { get; set; } = new List<List<CellData>>();
        }
        internal class StyleData
        {
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("backgroundColor")]
            public BackgroundColour BackgroundColor { get; set; } = BackgroundColour.None;

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("columnWidth")]
            public ColumnWidth ColumnWidth { get; set; } = ColumnWidth.None;
        }
        internal class CellData
        {
            [JsonProperty("rowIndex")]
            public int RowIndex { get; set; } = 0;

            [JsonProperty("columnIndex")]
            public int ColumnIndex { get; set; } = 0;

            [JsonProperty("value")]
            public string Value { get; set; } = string.Empty;
        }
        internal enum BackgroundColour
        {
            None,
            Primary,
            Secondary,
            Tertiary,
            OddEven,
            OddEvenReverse
        }
        internal enum ColumnWidth
        {
            None,
            Ten,
            Twenty,
            Thirty,
            Forty,
            Fifty,
            Sixty,
            Seventy,
            Eighty,
            Ninety
        }
    }
}
