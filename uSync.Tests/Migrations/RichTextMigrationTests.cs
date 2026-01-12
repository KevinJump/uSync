using Castle.Core.Logging;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Configuration.Models;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class RichTextMigrationTests : MigrationTestBase
{
    private RichTextEditorMigratingSerializer _serializer;

    [SetUp]
    public void Setup()
    {
        var loggerFactory = NullLoggerFactory.Instance;

        _serializer = new RichTextEditorMigratingSerializer(
            Mock.Of<IOptions<TinyMceToTiptapMigrationSettings>>(),
            loggerFactory.CreateLogger<RichTextEditorMigratingSerializer>());
    }

    private static string Source = @"{
  ""Blocks"": [
    {
      ""backgroundColor"": null,
      ""contentElementTypeKey"": ""a0eec50c-54ce-47d3-97f1-c01843887567"",
      ""displayInline"": false,
      ""editorSize"": ""medium"",
      ""forceHideContentEditorInOverlay"": false,
      ""iconColor"": null,
      ""label"": null,
      ""settingsElementTypeKey"": null,
      ""stylesheet"": null,
      ""thumbnail"": null,
      ""view"": null
    }
  ],
  ""Editor"": {
    ""toolbar"": [
      ""ace"",
      ""styles"",
      ""bold"",
      ""italic"",
      ""alignleft"",
      ""aligncenter"",
      ""alignright"",
      ""bullist"",
      ""numlist"",
      ""outdent"",
      ""indent"",
      ""link"",
      ""umbmediapicker"",
      ""umbmacro"",
      ""umbembeddialog""
    ],
    ""stylesheets"": [
        ""/Editor Styles.css""
    ],
    ""maxImageSize"": 500,
    ""mode"": ""classic"",
    ""dimensions"": {
      ""width"": 500,
      ""height"": 500
    }
  },
  ""HideLabel"": false,
  ""IgnoreUserStartNodes"": false,
  ""MediaParentId"": ""umb://media/71332aa78bea44f19aa600de961b66e8"",
  ""OverlaySize"": ""medium"",
  ""UseLiveEditing"": false
}";

    private static string Target = @"{
  ""blocks"": [
    {
      ""backgroundColor"": null,
      ""contentElementTypeKey"": ""a0eec50c-54ce-47d3-97f1-c01843887567"",
      ""displayInline"": false,
      ""editorSize"": ""medium"",
      ""forceHideContentEditorInOverlay"": false,
      ""iconColor"": null,
      ""label"": null,
      ""settingsElementTypeKey"": null,
      ""stylesheet"": null,
      ""thumbnail"": null,
      ""view"": null
    }
  ],
  ""dimensions"": {
    ""width"": 500,
    ""height"": 500
  },
  ""hideLabel"": false,
  ""ignoreUserStartNodes"": false,
  ""maxImageSize"": 500,
  ""mediaParentId"": ""71332aa7-8bea-44f1-9aa6-00de961b66e8"",
  ""mode"": ""classic"",
  ""overlaySize"": ""medium"",
  ""stylesheets"": [
    ""/Editor Styles.css""
  ],
  ""toolbar"": [
   ""ace"",
      ""styles"",
      ""bold"",
      ""italic"",
      ""alignleft"",
      ""aligncenter"",
      ""alignright"",
      ""bullist"",
      ""numlist"",
      ""outdent"",
      ""indent"",
      ""link"",
      ""umbmediapicker"",
      ""umbmacro"",
      ""umbembeddialog""
  ],
  ""useLiveEditing"": false
}";

    private static string TipTapTarget = @"{
  ""blocks"": [
    {
      ""backgroundColor"": null,
      ""contentElementTypeKey"": ""a0eec50c-54ce-47d3-97f1-c01843887567"",
      ""displayInline"": false,
      ""editorSize"": ""medium"",
      ""forceHideContentEditorInOverlay"": false,
      ""iconColor"": null,
      ""label"": null,
      ""settingsElementTypeKey"": null,
      ""stylesheet"": null,
      ""thumbnail"": null,
      ""view"": null
    }
  ],
  ""dimensions"": {
    ""width"": 500,
    ""height"": 500
  },
  ""extensions"": [
    ""Umb.Tiptap.RichTextEssentials"",
    ""Umb.Tiptap.Anchor"",
    ""Umb.Tiptap.Blockquote"",
    ""Umb.Tiptap.Bold"",
    ""Umb.Tiptap.BulletList"",
    ""Umb.Tiptap.CodeBlock"",
    ""Umb.Tiptap.Embed"",
    ""Umb.Tiptap.Figure"",
    ""Umb.Tiptap.Heading"",
    ""Umb.Tiptap.HorizontalRule"",
    ""Umb.Tiptap.HtmlAttributeClass"",
    ""Umb.Tiptap.HtmlAttributeDataset"",
    ""Umb.Tiptap.HtmlAttributeId"",
    ""Umb.Tiptap.HtmlAttributeStyle"",
    ""Umb.Tiptap.HtmlTagDiv"",
    ""Umb.Tiptap.HtmlTagSpan"",
    ""Umb.Tiptap.Image"",
    ""Umb.Tiptap.Italic"",
    ""Umb.Tiptap.Link"",
    ""Umb.Tiptap.MediaUpload"",
    ""Umb.Tiptap.OrderedList"",
    ""Umb.Tiptap.Strike"",
    ""Umb.Tiptap.Subscript"",
    ""Umb.Tiptap.Superscript"",
    ""Umb.Tiptap.Table"",
    ""Umb.Tiptap.TextAlign"",
    ""Umb.Tiptap.TextDirection"",
    ""Umb.Tiptap.TextIndent"",
    ""Umb.Tiptap.TrailingNode"",
    ""Umb.Tiptap.Underline"",
    ""Umb.Tiptap.WordCount"", 
    ""Umb.Tiptap.Block""
  ],
  ""ignoreUserStartNodes"": false,
  ""maxImageSize"": 500,
  ""mediaParentId"": ""71332aa7-8bea-44f1-9aa6-00de961b66e8"",
  ""overlaySize"": ""medium"",
  ""stylesheets"": [
    ""/Editor Styles.css""
  ],
  ""toolbar"": [
    [
      [
        ""Umb.Tiptap.Toolbar.StyleSelect"",
        ""Umb.Tiptap.Toolbar.Bold"",
        ""Umb.Tiptap.Toolbar.Italic"",
        ""Umb.Tiptap.Toolbar.TextAlignLeft"",
        ""Umb.Tiptap.Toolbar.TextAlignCenter"",
        ""Umb.Tiptap.Toolbar.TextAlignRight"",
        ""Umb.Tiptap.Toolbar.BulletList"",
        ""Umb.Tiptap.Toolbar.OrderedList"",
        ""Umb.Tiptap.Toolbar.TextOutdent"",
        ""Umb.Tiptap.Toolbar.TextIndent"",
        ""Umb.Tiptap.Toolbar.Link"",
        ""Umb.Tiptap.Toolbar.MediaPicker"",
        ""Umb.Tiptap.Toolbar.EmbeddedMedia""
      ]
    ]
  ],
  ""useLiveEditing"": false
}";

    [Test]
    public void RichTextMigrationValueTest()
        => TestSerializerPropertyMigration(_serializer, Source, TipTapTarget);

    [Test]
    public void RichTextMigratedValueTest()
        => TestSerializerPropertyMigration(_serializer, Target, TipTapTarget);

}