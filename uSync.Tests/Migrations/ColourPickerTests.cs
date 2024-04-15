using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class ColourPickerTests : MigrationTestBase
{
    private ColourPickerMigratingConfigSerializer _serializer = new ColourPickerMigratingConfigSerializer();

    private static string Source = "{\r\n  \"Items\": [\r\n    {\r\n      \"id\": 1,\r\n      \"value\": \"{\\\"value\\\":\\\"ff0000\\\",\\\"label\\\":\\\"Red\\\"}\"\r\n    },\r\n    {\r\n      \"id\": 2,\r\n      \"value\": \"{\\\"value\\\":\\\"00ff00\\\",\\\"label\\\":\\\"Green\\\"}\"\r\n    },\r\n    {\r\n      \"id\": 3,\r\n      \"value\": \"{\\\"value\\\":\\\"0000ff\\\",\\\"label\\\":\\\"Blue\\\"}\"\r\n    }\r\n  ],\r\n  \"UseLabel\": true\r\n}";
    private static string Target = @"{
  ""items"": [
    {
      ""label"": ""Red"",
      ""value"": ""ff0000""
    },
    {
      ""label"": ""Green"",
      ""value"": ""00ff00""
    },
    {
      ""label"": ""Blue"",
      ""value"": ""0000ff""
    }
  ],
  ""useLabel"": true
}";

	[Test]
	public void ColourPickerValuesTest()
        => TestSerializerPropertyMigration(_serializer, Source, Target);

    [Test]
    public void ColourPickerMigratedValueTest()
        => TestSerializerPropertyMigration(_serializer, Target, Target);

}
