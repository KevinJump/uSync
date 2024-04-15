using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class MultipleTextMigratorTests : MigrationTestBase
{
	private MultipleTextMigratingConfigSerializer _serializer = new MultipleTextMigratingConfigSerializer();

	private static string Source = "{\r\n \"Maximum\": 4,\r\n \"Minimum\": 1\r\n }";
	private static string Target = "{\r\n  \"max\": 4,\r\n  \"min\": 1\r\n}";

	[Test]
	public void MultipleTextMigratorValuesTest()
		=> TestSerializerPropertyMigration(_serializer, Source, Target);

	[Test]
	public void MultipleTextMigratedValuesTest()
		=> TestSerializerPropertyMigration(_serializer, Target, Target);
}
