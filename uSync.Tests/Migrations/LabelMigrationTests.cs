using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class LabelMigrationTests : MigrationTestBase
{
	private LabelMigratingConfigSerializer _serializer= new LabelMigratingConfigSerializer();

	private static string Source = "{ \"ValueType\": \"DECIMAL\" }";
	private static string Target = "{\r\n  \"umbracoDataValueType\": \"DECIMAL\"\r\n}";

	[Test]
	public void LabelMigrationValueTest()
		=> TestSerializerPropertyMigration(_serializer, Source, Target);

	[Test]
	public void LabelMigrationMigratedValueTest()
		=> TestSerializerPropertyMigration(_serializer, Target, Target);

}
