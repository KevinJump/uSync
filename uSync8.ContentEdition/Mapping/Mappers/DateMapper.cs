using System;

using Umbraco.Core.Services;

namespace uSync8.ContentEdition.Mapping.Mappers
{
    /// <summary>
    ///  DateTime Mapper
    /// </summary>
    /// <remarks>
    ///  While we don't need this for standard datatimes (they are converted in the core), 
    ///  things in other properties (like nested content) will not say they are a datatime
    ///  so its better to have the mapper catch them.
    /// </remarks>
    public class DateTimeMapper : SyncValueMapperBase, ISyncMapper
    {
        public DateTimeMapper(IEntityService entityService)
            : base(entityService) { }

        public override string Name => "Custom DateTimeMapper";

        public override string[] Editors => new string[] { "Umbraco.DateTime" };

        public override string GetExportValue(object value, string editorAlias)
        {
            // if it's a date, return it like one.
            if (value is DateTime date)
            {
                return date.ToString("s");
            }
            else if (DateTime.TryParse(value.ToString(), out date))
            {
                return date.ToString("s");
            }

            return value.ToString();
        }
    }
}
