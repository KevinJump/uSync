using System.Globalization;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Mapping;

/// <summary>
///  DateTime Mapper
/// </summary>
/// <remarks>
///  While we don't need this for standard date-times (they are converted in the core), 
///  things in other properties (like nested content) will not say they are a date-time
///  so its better to have the mapper catch them.
/// </remarks>
public class DateTimeMapper : SyncValueMapperBase, ISyncMapper
{
    public DateTimeMapper(IEntityService entityService)
        : base(entityService) { }

    public override string Name => "Custom DateTimeMapper";

    public override string[] Editors => [Constants.PropertyEditors.Aliases.DateTime];

    public override string? GetImportValue(string value, string editorAlias)
        => GetFormattedDateTime(value);

    public override string? GetExportValue(object value, string editorAlias)
        => GetFormattedDateTime(value);

    private static string? GetFormattedDateTime(object value)
    {
        // if it's a date, return it like one.
        if (value is DateTime date)
        {
            return date.ToString("s");
        }

        // try and read it exactly, as a sortable date.
        var culture = new CultureInfo("en-US");
        if (DateTime.TryParseExact(value.ToString(), "s", culture, DateTimeStyles.None, out date))
        {
            return date.ToString("s");
        }

        if (DateTime.TryParse(value.ToString(), out date))
        {
            return date.ToString("s");
        }

        return value?.ToString();
    }
}
