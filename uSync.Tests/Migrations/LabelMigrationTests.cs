using NUnit.Framework;

using Umbraco.Cms.Core;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class LabelMigrationTests : MigrationTestBase
{
    private LabelMigratingConfigSerializer _serializer = new LabelMigratingConfigSerializer();

    private static string Source = "{ \"ValueType\": \"DECIMAL\" }";
    private static string V18Source = "{\r\n  \"umbracoDataValueType\": \"DECIMAL\"\r\n}";

    // v19: the value type is the editor, so it's no longer in the config.
    private static string Target = "{}";

    [Test]
    public void LabelMigrationValueTest()
        => TestSerializerPropertyMigration(_serializer, Source, Target);

    [Test]
    public void LabelMigrationV18ValueTest()
        => TestSerializerPropertyMigration(_serializer, V18Source, Target);

    [Test]
    public void LabelMigrationMigratedValueTest()
        => TestSerializerPropertyMigration(_serializer, Target, Target);

    [TestCase("DECIMAL", Constants.PropertyEditors.Aliases.LabelDecimal)]
    [TestCase("INT", Constants.PropertyEditors.Aliases.LabelInteger)]
    [TestCase("BIGINT", Constants.PropertyEditors.Aliases.LabelBigInt)]
    [TestCase("DATETIME", Constants.PropertyEditors.Aliases.LabelDateTime)]
    [TestCase("DATE", Constants.PropertyEditors.Aliases.LabelDateTime)]
    [TestCase("TIME", Constants.PropertyEditors.Aliases.LabelTime)]
    [TestCase("TEXT", Constants.PropertyEditors.Aliases.LabelText)]
    [TestCase("STRING", null)]
    public void LabelValueTypeReplacementTest(string valueType, string? expectedEditor)
        => TestSerializerEditorReplacement(_serializer, Constants.PropertyEditors.Aliases.Label,
            $"{{ \"umbracoDataValueType\": \"{valueType}\" }}", expectedEditor);

    [Test]
    public void LabelLegacyValueTypeReplacementTest()
        => TestSerializerEditorReplacement(_serializer, Constants.PropertyEditors.Aliases.Label,
            Source, Constants.PropertyEditors.Aliases.LabelDecimal);

    [Test]
    public void LabelWithoutValueTypeIsNotReplacedTest()
        => TestSerializerEditorReplacement(_serializer, Constants.PropertyEditors.Aliases.Label, Target, null);
}
