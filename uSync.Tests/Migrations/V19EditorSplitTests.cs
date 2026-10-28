using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

/// <summary>
///  v19 splits some editors by their config, and moves min/max config to a range.
///  older files should import the same way an upgraded Umbraco site migrates them.
/// </summary>
[TestFixture]
internal class V19EditorSplitTests : MigrationTestBase
{
    private MediaPickerConfigSerializer _mediaPicker;
    private MultiNodeTreePickerMigratingConfigSerializer _mntp;

    private readonly DropDownMigratingConfigSerializer _dropDown = new();
    private readonly MultiUrlPickerMigratingConfigSerializer _urlPicker = new();
    private readonly NumericMigratingConfigSerializer _numeric = new();
    private readonly BlockGridMigratingConfigSerializer _blockGrid = new();

    [SetUp]
    public void Setup()
    {
        _mediaPicker = new MediaPickerConfigSerializer(Mock.Of<IMediaTypeService>());
        _mntp = new MultiNodeTreePickerMigratingConfigSerializer(
            Mock.Of<IContentTypeService>(), Mock.Of<IMediaTypeService>(), Mock.Of<IMemberTypeService>());
    }

    // media picker

    [Test]
    public void SingleMediaPickerIsReplacedTest()
        => TestSerializerEditorReplacement(_mediaPicker, Constants.PropertyEditors.Aliases.MediaPicker3,
            "{ \"multiple\": false }", Constants.PropertyEditors.Aliases.SingleMediaPicker);

    [Test]
    public void MultipleMediaPickerIsNotReplacedTest()
        => TestSerializerEditorReplacement(_mediaPicker, Constants.PropertyEditors.Aliases.MediaPicker3,
            "{ \"multiple\": true }", null);

    // v19 doesn't write 'multiple', so a missing key must not move a v19 multiple picker.
    [Test]
    public void MediaPickerWithoutMultipleIsNotReplacedTest()
        => TestSerializerEditorReplacement(_mediaPicker, Constants.PropertyEditors.Aliases.MediaPicker3,
            "{ \"validationLimit\": { \"max\": 3 } }", null);

    [Test]
    public void SingleMediaPickerConfigTest()
        => TestSerializerPropertyMigration(_mediaPicker,
            "{ \"multiple\": false, \"validationLimit\": { \"min\": 1, \"max\": 1 } }", "{}");

    [Test]
    public void MultipleMediaPickerConfigTest()
        => TestSerializerPropertyMigration(_mediaPicker,
            "{ \"multiple\": true, \"validationLimit\": { \"max\": 3 } }",
            "{ \"validationLimit\": { \"max\": 3 } }");

    // dropdown

    [Test]
    public void SingleDropDownIsReplacedTest()
        => TestSerializerEditorReplacement(_dropDown, Constants.PropertyEditors.Aliases.MultipleDropDown,
            "{ \"multiple\": false, \"items\": [\"one\", \"two\"] }", Constants.PropertyEditors.Aliases.SingleDropDown);

    [Test]
    public void MultipleDropDownIsNotReplacedTest()
        => TestSerializerEditorReplacement(_dropDown, Constants.PropertyEditors.Aliases.MultipleDropDown,
            "{ \"multiple\": true }", null);

    [Test]
    public void DropDownWithoutMultipleIsNotReplacedTest()
        => TestSerializerEditorReplacement(_dropDown, Constants.PropertyEditors.Aliases.MultipleDropDown,
            "{ \"items\": [\"one\", \"two\"] }", null);

    [Test]
    public void DropDownConfigTest()
        => TestSerializerPropertyMigration(_dropDown,
            "{ \"items\": [\"one\", \"two\"], \"multiple\": true }",
            "{ \"items\": [\"one\", \"two\"] }");

    // url picker

    [Test]
    public void SingleUrlPickerIsReplacedTest()
        => TestSerializerEditorReplacement(_urlPicker, Constants.PropertyEditors.Aliases.MultiUrlPicker,
            "{ \"maxNumber\": 1 }", Constants.PropertyEditors.Aliases.SingleUrlPicker);

    [TestCase("{ \"maxNumber\": 0 }")]
    [TestCase("{ \"maxNumber\": 3 }")]
    [TestCase("{ \"validationLimit\": { \"max\": 1 } }")]
    public void MultiUrlPickerIsNotReplacedTest(string config)
        => TestSerializerEditorReplacement(_urlPicker, Constants.PropertyEditors.Aliases.MultiUrlPicker, config, null);

    [Test]
    public void SingleUrlPickerConfigTest()
        => TestSerializerPropertyMigration(_urlPicker,
            "{ \"hideAnchor\": true, \"maxNumber\": 1, \"minNumber\": 1 }",
            "{ \"hideAnchor\": true }");

    [Test]
    public void MultiUrlPickerConfigTest()
        => TestSerializerPropertyMigration(_urlPicker,
            "{ \"hideAnchor\": true, \"maxNumber\": 3, \"minNumber\": 0 }",
            "{ \"hideAnchor\": true, \"validationLimit\": { \"max\": 3 } }");

    // numeric

    [Test]
    public void NumericConfigTest()
        => TestSerializerPropertyMigration(_numeric,
            "{ \"max\": 10, \"min\": 2, \"step\": 1 }",
            "{ \"step\": 1, \"validationRange\": { \"min\": 2, \"max\": 10 } }");

    [Test]
    public void NumericMigratedConfigTest()
        => TestSerializerPropertyMigration(_numeric,
            "{ \"step\": 1, \"validationRange\": { \"min\": 2, \"max\": 10 } }",
            "{ \"step\": 1, \"validationRange\": { \"min\": 2, \"max\": 10 } }");

    // mntp stays on its editor, but its limits move.

    [Test]
    public void MultiNodeTreePickerIsNotReplacedTest()
        => TestSerializerEditorReplacement(_mntp, Constants.PropertyEditors.Aliases.MultiNodeTreePicker,
            "{ \"maxNumber\": 1, \"minNumber\": 0 }", null);

    [Test]
    public void MultiNodeTreePickerConfigTest()
        => TestSerializerPropertyMigration(_mntp,
            "{ \"filter\": null, \"maxNumber\": 5, \"minNumber\": 1 }",
            "{ \"filter\": null, \"validationLimit\": { \"min\": 1, \"max\": 5 } }");

    // block grid areas

    [Test]
    public void BlockGridAreaConfigTest()
        => TestSerializerPropertyMigration(_blockGrid,
            "{ \"blocks\": [ { \"contentElementTypeKey\": \"a\", \"areas\": [ { \"alias\": \"main\", \"minAllowed\": 0, \"maxAllowed\": 2 } ] } ] }",
            "{ \"blocks\": [ { \"contentElementTypeKey\": \"a\", \"areas\": [ { \"alias\": \"main\", \"validationLimit\": { \"min\": 0, \"max\": 2 } } ] } ] }");

    [Test]
    public void BlockGridMigratedConfigTest()
        => TestSerializerPropertyMigration(_blockGrid,
            "{ \"blocks\": [ { \"areas\": [ { \"alias\": \"main\", \"validationLimit\": { \"max\": 2 } } ] } ] }",
            "{ \"blocks\": [ { \"areas\": [ { \"alias\": \"main\", \"validationLimit\": { \"max\": 2 } } ] } ] }");
}
