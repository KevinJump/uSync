using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class TagMigrationTest : MigrationTestBase
{
	private TagMigratingConfigSerializer _serializer = new TagMigratingConfigSerializer();

	private static string Source = @"{
  ""Delimiter"": ""\u0000"",
  ""Group"": ""taggroup"",
  ""StorageType"": 0
}";
	private static string Target = @"{
  ""group"": ""taggroup"",
  ""storageType"": [
    ""csv""
  ]
}";

	[Test]
	public void TagValueMigrationTest()
		=> TestSerializerPropertyMigration(_serializer, Source, Target);

	[Test]

	public void TagValueMigratedValuesTest()
		=> TestSerializerPropertyMigration(_serializer, Target, Target);
}