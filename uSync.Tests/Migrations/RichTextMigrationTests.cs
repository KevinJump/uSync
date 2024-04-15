using NUnit.Framework;

using uSync.Core.Serialization.Serializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class RichTextMigrationTests: MigrationTestBase
{
	private RichTextEditorMigratingSerializer _serializer = new();

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
  ""mediaParentId"": [
    {
      ""crops"": [],
      ""focalPoint"": null,
      ""key"": ""5f6d6564-6961-5f70-6172-656e745f756d"",
      ""mediaKey"": ""71332aa7-8bea-44f1-9aa6-00de961b66e8"",
      ""mediaTypeAlias"": """"
    }
  ],
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

	[Test]
	public void RichTextMigrationValueTest()
		=> TestSerializerPropertyMigration(_serializer, Source, Target);

	[Test]
	public void RichTextMigratedValueTest()
		=> TestSerializerPropertyMigration(_serializer, Target, Target);

}