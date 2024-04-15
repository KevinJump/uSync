using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class SliderMigrationTests : MigrationTestBase
{
	private SliderMigratingConfigSerializer _serializer = new SliderMigratingConfigSerializer();

	private static string Source = "{\r\n  \"EnableRange\": false,\r\n  \"InitialValue\": 1,\r\n  \"InitialValue2\": 2,\r\n  \"MaximumValue\": 5,\r\n  \"MinimumValue\": 1,\r\n  \"StepIncrements\": 1\r\n}";
	private static string Target = "{\r\n  \"enableRange\": false,\r\n  \"initVal1\": 1,\r\n  \"initVal2\": 2,\r\n  \"maxVal\": 5,\r\n  \"minVal\": 1,\r\n  \"step\": 1\r\n}";

	[Test]
	public void SliderMigrationValueTest()
		=> TestSerializerPropertyMigration(_serializer, Source, Target);

	[Test]
	public void SliderMigratedValueTest()
		=> TestSerializerPropertyMigration(_serializer, Target, Target);
}
