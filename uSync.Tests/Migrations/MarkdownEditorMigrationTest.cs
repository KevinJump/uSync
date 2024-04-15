using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class MarkdownEditorMigrationTest : MigrationTestBase
{
	private MarkdownMigratingConfigSerializer _serializer = new MarkdownMigratingConfigSerializer();

	private static string Source = "{\r\n  \"DefaultValue\": \"#Hello\",\r\n  \"DisplayLivePreview\": false,\r\n  \"OverlaySize\": \"medium\"\r\n}";
	private static string Target = "{\r\n  \"defaultValue\": \"#Hello\",\r\n  \"overlaySize\": \"medium\",\r\n  \"preview\": false\r\n}";

	[Test]
	public void MarkdownSerializeTest()
		=> TestSerializerPropertyMigration(_serializer, Source, Target);
	[Test]
	public void MarkdownMigratedValueTest()
		=> TestSerializerPropertyMigration(_serializer, Target, Target);
}
