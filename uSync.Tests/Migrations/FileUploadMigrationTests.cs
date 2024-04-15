using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class FileUploadMigrationTests : MigrationTestBase
{
    private FileUploadMigratingConfigSerializer _serializer = new();

	private static string Source = @"{
  ""FileExtensions"": [
    {
      ""id"": 0,
      ""value"": ""pdf""
    },
    {
      ""id"": 1,
      ""value"": ""png""
    }
  ]
}";

	private static string Target = @"{
  ""fileExtensions"": [
    ""pdf"",
    ""png""
  ]
}";


	[Test]
	public void FileMigrationValueTest()
		=> TestSerializerPropertyMigration(_serializer, Source, Target);

    [Test]
    public void FileUploadMigratedValueTest()
        => TestSerializerPropertyMigration(_serializer, Target, Target);
}
