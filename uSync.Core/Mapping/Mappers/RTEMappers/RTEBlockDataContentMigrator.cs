using Microsoft.Extensions.Logging;

using System.Text.RegularExpressions;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
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
public class RTEBlockDataContentMigrator : SyncBlockMapperBase<RichTextBlockValue>, ISyncMapper
{
    public RTEBlockDataContentMigrator(
        IEntityService entityService,
        IContentTypeService contentTypeService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        ILogger<RTEBlockDataContentMigrator> logger)
        : base(entityService, contentTypeService, mapperCollection, logger)
    { }

    /// <inheritdoc />
    public override string Name => "RTE Block Data Content Mapper";

    /// <inheritdoc />
    public override string[] Editors => [
        "Umbraco.TinyMCE",
        Constants.PropertyEditors.Aliases.RichText,
        $"{Constants.PropertyEditors.Aliases.Grid}.rte"
    ];

    /// <inheritdoc />
    public override async Task<string?> GetImportValueAsync(string value, string editorAlias)
    {
        if (value.TryDeserialize<RichTextEditorValue>(out RichTextEditorValue? richTextEditorValue) is false || richTextEditorValue is null)
            return await base.GetImportValueAsync(value, editorAlias);

        richTextEditorValue.Markup = MigrateRTEMarkupBlocks(richTextEditorValue.Markup);

        if (richTextEditorValue.Blocks is not null && richTextEditorValue.Blocks?.ContentData.Count > 0)
        {
            var blockJson = await base.GetImportValueAsync(richTextEditorValue.Blocks.SerializeJsonString(), editorAlias);
            if (blockJson is not null)
            {
                richTextEditorValue.Blocks = blockJson.DeserializeJson<RichTextBlockValue>();
            }
        }

        return richTextEditorValue.SerializeJsonString();
    }

    /// <inheritdoc />
    public override async Task<string?> GetExportValueAsync(object value, string editorAlias)
    {
        var stringValue = value?.ToString() ?? string.Empty;
        if (stringValue.TryDeserialize<RichTextEditorValue>(out RichTextEditorValue? richTextEditorValue) is false || richTextEditorValue is null)
            return stringValue;

        if (richTextEditorValue.Blocks is not null)
        {
            var blockJson = await base.GetExportValueAsync(richTextEditorValue.Blocks.SerializeJsonString(), editorAlias);
            if (blockJson is not null)
            {
                richTextEditorValue.Blocks = blockJson.DeserializeJson<RichTextBlockValue>();

            }
        }

        return richTextEditorValue.SerializeJsonString(true);
    }

    /// <summary>
    ///  checks to see if the markup contains any html for block elements, and if so, migrates the data-content-udi attributes to data-content-key.
    /// </summary>
    private string MigrateRTEMarkupBlocks(string markup)
    {
        if (RTEBlockHelper.BlockRegex().IsMatch(markup) is false)
            return markup;

        return RTEBlockHelper.BlockRegex().Replace(
            markup,
            match => UdiParser.TryParse(match.Groups["udi"].Value, out GuidUdi? guidUdi)
                ? match.Value
                    .Replace(match.Groups["attribute"].Value, "data-content-key")
                    .Replace(match.Groups["udi"].Value, guidUdi.Guid.ToString("D"))
                : string.Empty);
    }

    /// <summary>
    ///  dependency check is done in the core RTE mapper, (which just calls the mapperCollection for blocks.
    /// </summary>
    public override Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
        => Task.FromResult<IEnumerable<uSyncDependency>>([]);
}
