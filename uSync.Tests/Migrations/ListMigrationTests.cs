using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class ListMigrationTests : MigrationTestBase
{
	private DataListMigratingConfigSerializer _serializer = new DataListMigratingConfigSerializer();

	private static string Source = "{\r\n\t\"Items\": [\r\n\t\t{\r\n\t\t\t\"id\": 1,\r\n\t\t\t\"value\": \"One\"\r\n\t\t},\r\n\t\t{\r\n\t\t\t\"id\": 2,\r\n\t\t\t\"value\": \"Two\"\r\n\t\t},\r\n\t\t{\r\n\t\t\t\"id\": 3,\r\n\t\t\t\"value\": \"Three\"\r\n\t\t}\r\n\t]\r\n}\r\n";
	private static string Target = "{\r\n  \"items\": [\r\n    \"One\",\r\n    \"Two\",\r\n    \"Three\"\r\n  ]\r\n}";

	[Test]
	public void DropdownListMigrationTest()
		=> TestSerializerPropertyMigration(_serializer, Source, Target);

	[Test]
	public void DropdownListMigratedValueTest()
		=> TestSerializerPropertyMigration(_serializer, Target, Target);
}
