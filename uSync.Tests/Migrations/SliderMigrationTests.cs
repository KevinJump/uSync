using NUnit.Framework;

using Umbraco.Cms.Core;

using uSync.Core.DataTypes.DataTypeSerializers;

namespace uSync.Tests.Migrations;

[TestFixture]
internal class SliderMigrationTests : MigrationTestBase
{
    private SliderMigratingConfigSerializer _serializer = new SliderMigratingConfigSerializer();

    private static string Source = "{\r\n  \"EnableRange\": false,\r\n  \"InitialValue\": 1,\r\n  \"InitialValue2\": 2,\r\n  \"MaximumValue\": 5,\r\n  \"MinimumValue\": 1,\r\n  \"StepIncrements\": 1\r\n}";
    private static string V18Source = "{\r\n  \"enableRange\": false,\r\n  \"initVal1\": 1,\r\n  \"initVal2\": 2,\r\n  \"maxVal\": 5,\r\n  \"minVal\": 1,\r\n  \"step\": 1\r\n}";
    private static string Target = "{\r\n  \"initVal1\": 1,\r\n  \"initVal2\": 2,\r\n  \"step\": 1,\r\n  \"validationRange\": { \"min\": 1, \"max\": 5 }\r\n}";

    // a zero minimum is a real bound on a slider, a zero maximum is unbounded.
    private static string ZeroSource = "{ \"minVal\": 0, \"maxVal\": 0 }";
    private static string ZeroTarget = "{ \"validationRange\": { \"min\": 0 } }";

    [Test]
    public void SliderMigrationValueTest()
        => TestSerializerPropertyMigration(_serializer, Source, Target);

    [Test]
    public void SliderV18ValueTest()
        => TestSerializerPropertyMigration(_serializer, V18Source, Target);

    [Test]
    public void SliderMigratedValueTest()
        => TestSerializerPropertyMigration(_serializer, Target, Target);

    [Test]
    public void SliderZeroBoundsTest()
        => TestSerializerPropertyMigration(_serializer, ZeroSource, ZeroTarget);

    [Test]
    public void SliderWithRangeIsReplacedTest()
        => TestSerializerEditorReplacement(_serializer, Constants.PropertyEditors.Aliases.Slider,
            "{ \"enableRange\": true }", Constants.PropertyEditors.Aliases.RangeSlider);

    [Test]
    public void SliderWithoutRangeIsNotReplacedTest()
        => TestSerializerEditorReplacement(_serializer, Constants.PropertyEditors.Aliases.Slider, V18Source, null);
}
