using System;
using System.Text.Json.Nodes;
using System.Xml.Linq;

using NUnit.Framework;

using uSync.BackOffice.Legacy.Migrators;

namespace uSync.Tests.Migrations;

/// <summary>
///  migrators that move legacy files onto the v19 editors as they are copied.
/// </summary>
[TestFixture]
internal class LegacyMigratorTests
{
    private static XElement DataType(string editorAlias, string config)
        => XElement.Parse(
            $"<DataType Key=\"{Guid.NewGuid()}\"><Info><Name>test</Name><EditorAlias>{editorAlias}</EditorAlias><EditorUIAlias>ui</EditorUIAlias></Info><Config><![CDATA[{config}]]></Config></DataType>");

    private static string EditorAlias(XElement node) => node.Element("Info")!.Element("EditorAlias")!.Value;

    private static JsonObject Config(XElement node) => (JsonObject)JsonNode.Parse(node.Element("Config")!.Value)!;

    // media picker and dropdown without a 'multiple' key

    [TestCase("{}", 18, true)]
    [TestCase("{\"multiple\":false}", 18, true)]
    [TestCase("{\"multiple\":true}", 18, false)]
    [TestCase("{}", 19, false)]
    public void MediaPickerSingleAppliesTest(string config, int version, bool expected)
        => Assert.That(new MediaPickerSingleLegacyMigrator().AppliesTo(DataType("Umbraco.MediaPicker3", config), version), Is.EqualTo(expected));

    [Test]
    public void MediaPickerSingleMigratesAndDropsLimitTest()
    {
        var node = DataType("Umbraco.MediaPicker3", "{\"validationLimit\":{\"max\":1},\"crops\":[]}");

        new MediaPickerSingleLegacyMigrator().MigrateDataType(node);

        Assert.That(EditorAlias(node), Is.EqualTo("Umbraco.MediaPicker.Single"));
        Assert.That(Config(node).ContainsKey("validationLimit"), Is.False);
        Assert.That(Config(node).ContainsKey("crops"), Is.True);
    }

    [Test]
    public void DropDownSingleMigratesTest()
    {
        var node = DataType("Umbraco.DropDown.Flexible", "{\"items\":[\"a\"]}");
        var migrator = new DropDownSingleLegacyMigrator();

        Assert.That(migrator.AppliesTo(node, 18), Is.True);
        migrator.MigrateDataType(node);

        Assert.That(EditorAlias(node), Is.EqualTo("Umbraco.DropDown.Single"));
    }

    // multi node tree picker

    [TestCase("content", "{\"maxNumber\":1}", "Umbraco.ContentPicker")]
    [TestCase("content", "{\"maxNumber\":0}", "Umbraco.DocumentPicker.Multiple")]
    [TestCase("media", "{\"maxNumber\":1}", "Umbraco.MediaPicker.Single")]
    [TestCase("media", "{\"maxNumber\":5}", "Umbraco.MediaPicker3")]
    [TestCase("member", "{\"maxNumber\":1}", "Umbraco.MemberPicker")]
    [TestCase("member", "{}", "Umbraco.MemberPicker.Multiple")]
    public void MntpTargetTest(string treeType, string limits, string expected)
    {
        var limitJson = limits.Trim('{', '}');
        var config = $"{{\"treeSource\":{{\"type\":\"{treeType}\"}}{(limitJson.Length > 0 ? "," + limitJson : "")}}}";
        var node = DataType("Umbraco.MultiNodeTreePicker", config);

        new MultiNodeTreePickerLegacyMigrator().MigrateDataType(node);

        Assert.That(EditorAlias(node), Is.EqualTo(expected));
    }

    [Test]
    public void MntpConfigTest()
    {
        var node = DataType("Umbraco.MultiNodeTreePicker",
            "{\"treeSource\":{\"type\":\"media\",\"id\":\"umb://media/0f4f1f2e3d4c4b5a8e9f0a1b2c3d4e5f\"},\"minNumber\":1,\"maxNumber\":5,\"ignoreUserStartNodes\":true}");

        new MultiNodeTreePickerLegacyMigrator().MigrateDataType(node);

        var config = Config(node);
        Assert.That(config["startNodeId"]!.ToString(), Is.EqualTo("0f4f1f2e-3d4c-4b5a-8e9f-0a1b2c3d4e5f"));
        Assert.That(config["validationLimit"]!["min"]!.GetValue<int>(), Is.EqualTo(1));
        Assert.That(config["validationLimit"]!["max"]!.GetValue<int>(), Is.EqualTo(5));
        Assert.That(config["ignoreUserStartNodes"]!.GetValue<bool>(), Is.True);
        Assert.That(config.ContainsKey("treeSource"), Is.False);
    }

    [Test]
    public void MntpMediaValueIsConvertedToJsonTest()
    {
        var migrator = new MultiNodeTreePickerLegacyMigrator();
        var node = DataType("Umbraco.MultiNodeTreePicker", "{\"treeSource\":{\"type\":\"media\"}}");
        var migration = migrator.MigrateDataType(node)!;

        var value = migrator.MigrateValue("umb://media/0f4f1f2e3d4c4b5a8e9f0a1b2c3d4e5f", migration);

        var items = (JsonArray)JsonNode.Parse(value)!;
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0]!["mediaKey"]!.ToString(), Is.EqualTo("0f4f1f2e-3d4c-4b5a-8e9f-0a1b2c3d4e5f"));
    }

    [Test]
    public void MntpContentValueIsUnchangedTest()
    {
        var migrator = new MultiNodeTreePickerLegacyMigrator();
        var node = DataType("Umbraco.MultiNodeTreePicker", "{\"treeSource\":{\"type\":\"content\"}}");
        var migration = migrator.MigrateDataType(node)!;

        const string value = "umb://document/0f4f1f2e3d4c4b5a8e9f0a1b2c3d4e5f";
        Assert.That(migrator.MigrateValue(value, migration), Is.EqualTo(value));
    }
}
