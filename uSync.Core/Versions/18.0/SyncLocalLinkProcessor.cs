using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace uSync.Core.Versions._18._0;

public interface ISyncTypedLocalLinkProcessor
{
    public Type PropertyEditorValueType { get; }

    public IEnumerable<string> PropertyEditorAliases { get; }

    public Func<object?, Func<object?, bool>, Func<string, string>, bool> Process { get; }
}

/// <summary>
///  Finds and replaces the legacy {localLink:x} syntax within values, mapping ids/UDIs to keys.
/// </summary>
/// <remarks>
///  Umbraco's own <c>HtmlLocalLinkParser.FindLegacyLocalLinkIds</c> (and its <c>LocalLinkTag</c> model)
///  are obsolete and scheduled for removal in Umbraco 18, but uSync still needs to detect these
///  legacy (un-migrated) links when mapping content, so the parsing logic lives here instead.
/// </remarks>
public partial class SyncLocalLinkProcessor
{
    // copied from Umbraco.Cms.Core.Templates.HtmlLocalLinkParser.LocalLinkPattern
    [GeneratedRegex(@"href=['""](?<locallink>\/?(?:\{|\%7B)localLink:(?<guid>[a-zA-Z0-9-://]+)(?:\}|\%7D))", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace, "en-GB")]
    private static partial Regex LegacyLocalLinkPattern();

    private readonly IIdKeyMap _idKeyMap;
    private readonly IEnumerable<ISyncTypedLocalLinkProcessor> _localLinkProcessors;

    public SyncLocalLinkProcessor(
        IIdKeyMap idKeyMap,
        IEnumerable<ISyncTypedLocalLinkProcessor> localLinkProcessors)
    {
        _idKeyMap = idKeyMap;
        _localLinkProcessors = localLinkProcessors;
    }

    public IEnumerable<string> GetSupportedPropertyEditorAliases() =>
        _localLinkProcessors.SelectMany(p => p.PropertyEditorAliases);

    public bool ProcessToEditorValue(object? editorValue)
    {
        ISyncTypedLocalLinkProcessor? processor =
            _localLinkProcessors.FirstOrDefault(p => p.PropertyEditorValueType == editorValue?.GetType());

        return processor is not null && processor.Process.Invoke(editorValue, ProcessToEditorValue, ProcessStringValue);
    }

    public string ProcessStringValue(string input)
    {
        // find all legacy tags
        var tags = FindLegacyLocalLinkIds(input).ToList();

        foreach (LocalLinkTag tag in tags)
        {
            string newTagHref;
            if (tag.Udi is not null)
            {
                newTagHref = tag.TagHref.Replace(tag.Udi.ToString(), tag.Udi.Guid.ToString())
                             + $"\" type=\"{tag.Udi.EntityType}";
            }
            else if (tag.IntId is not null)
            {
                // try to get the key and type from the int, else do nothing
                (Guid Key, string EntityType)? conversionResult = CreateIntBasedKeyType(tag.IntId.Value);
                if (conversionResult is null)
                {
                    continue;
                }

                newTagHref = tag.TagHref.Replace(tag.IntId.Value.ToString(), conversionResult.Value.Key.ToString())
                             + $"\" type=\"{conversionResult.Value.EntityType}";
            }
            else
            {
                // tag does not contain enough information to convert
                continue;
            }

            input = input.Replace(tag.TagHref, newTagHref);
        }

        return input;
    }

    private (Guid Key, string EntityType)? CreateIntBasedKeyType(int id)
    {
        // very old data, best effort replacement
        Attempt<Guid> documentAttempt = _idKeyMap.GetKeyForId(id, UmbracoObjectTypes.Document);
        if (documentAttempt.Success)
        {
            return (Key: documentAttempt.Result, EntityType: UmbracoObjectTypes.Document.ToString());
        }

        Attempt<Guid> mediaAttempt = _idKeyMap.GetKeyForId(id, UmbracoObjectTypes.Media);
        if (mediaAttempt.Success)
        {
            return (Key: mediaAttempt.Result, EntityType: UmbracoObjectTypes.Media.ToString());
        }

        return null;
    }

    // copied from Umbraco.Cms.Core.Templates.HtmlLocalLinkParser.FindLegacyLocalLinkIds
    private static IEnumerable<LocalLinkTag> FindLegacyLocalLinkIds(string text)
    {
        MatchCollection tags = LegacyLocalLinkPattern().Matches(text);
        foreach (Match tag in tags)
        {
            if (tag.Groups.Count <= 0)
            {
                continue;
            }

            var id = tag.Groups["guid"].Value;

            // The id could be an int or a UDI
            if (UdiParser.TryParse(id, out Udi? udi))
            {
                if (udi is GuidUdi guidUdi)
                {
                    yield return new LocalLinkTag(null, guidUdi, tag.Groups["locallink"].Value);
                }
            }

            if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intId))
            {
                yield return new LocalLinkTag(intId, null, tag.Groups["locallink"].Value);
            }
        }
    }

    // copied from Umbraco.Cms.Core.Templates.HtmlLocalLinkParser.LocalLinkTag
    private sealed class LocalLinkTag
    {
        public LocalLinkTag(int? intId, GuidUdi? udi, string tagHref)
        {
            IntId = intId;
            Udi = udi;
            TagHref = tagHref;
        }

        public int? IntId { get; }

        public GuidUdi? Udi { get; }

        public string TagHref { get; }
    }
}
