using System.Text.RegularExpressions;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

using uSync.Core.Extensions;

namespace uSync.Core.Mapping.Mappers.RTEMappers;

public partial class RTEBlockHelper
{
    [GeneratedRegex("<umb-rte-block.*(?<attribute>data-content-udi)=\"(?<udi>.[^\"]*)\".*<\\/umb-rte-block")]
    public static partial Regex BlockRegex();
}

/// <summary>
///  updates inline block data-content-udi attributes to data-content-key
/// </summary>
public class RTEBlockDataContentMigrator : SyncValueMapperBase, ISyncMapper
{
    public RTEBlockDataContentMigrator(IEntityService entityService)
        : base(entityService)
    { }

    public override string Name => "RTE Block Data Content Mapper";

    public override string[] Editors => [
        "Umbraco.TinyMCE",
        Constants.PropertyEditors.Aliases.RichText,
        $"{Constants.PropertyEditors.Aliases.Grid}.rte"
    ];

    public override Task<string?> GetImportValueAsync(string value, string editorAlias)
    {
        if (value.TryDeserialize<RichTextEditorValue>(out RichTextEditorValue? richTextEditorValue) is false || richTextEditorValue is null)
            return base.GetImportValueAsync(value, editorAlias);

        if (RTEBlockHelper.BlockRegex().IsMatch(richTextEditorValue.Markup) is false)
            return base.GetImportValueAsync(value, editorAlias);

        richTextEditorValue.Markup = RTEBlockHelper.BlockRegex().Replace(
        richTextEditorValue.Markup,
        match => UdiParser.TryParse(match.Groups["udi"].Value, out GuidUdi? guidUdi)
            ? match.Value
                .Replace(match.Groups["attribute"].Value, "data-content-key")
                .Replace(match.Groups["udi"].Value, guidUdi.Guid.ToString("D"))
            : string.Empty);

        return Task.FromResult<string?>(richTextEditorValue.SerializeJsonString());
    }
}
