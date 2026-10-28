using NUnit.Framework;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class MultipleTextMigratorTests : MigrationTestBase
{
    private MultipleTextMigratingConfigSerializer _serializer = new MultipleTextMigratingConfigSerializer();

    private static string Source = "{\r\n \"Maximum\": 4,\r\n \"Minimum\": 1\r\n }";
    private static string V18Source = "{\r\n  \"max\": 4,\r\n  \"min\": 1\r\n}";
    private static string Target = "{ \"validationLimit\": { \"min\": 1, \"max\": 4 } }";

    // zero means no limit, so a 0/0 config has no limit at all.
    private static string ZeroSource = "{ \"max\": 0, \"min\": 0 }";

    [Test]
    public void MultipleTextMigratorValuesTest()
        => TestSerializerPropertyMigration(_serializer, Source, Target);

    [Test]
    public void MultipleTextV18ValuesTest()
        => TestSerializerPropertyMigration(_serializer, V18Source, Target);

    [Test]
    public void MultipleTextMigratedValuesTest()
        => TestSerializerPropertyMigration(_serializer, Target, Target);

    [Test]
    public void MultipleTextZeroValuesTest()
        => TestSerializerPropertyMigration(_serializer, ZeroSource, "{}");
}
