using Jumoo.Json;

using NUnit.Framework;

using System;
using System.Text.Json.Nodes;
using System.Xml.Linq;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.Blocks;

namespace uSync.Tests.Extensions;

/// <summary>
///  uSync detects changes by comparing serialized json, so which converter handles which type
///  decides whether an item reports as changed. These are the converters uSync's serialization
///  used before the json helpers moved to Jumoo.Json, and it has to stay that way.
/// </summary>
/// <remarks>
///  Jumoo.Json's options are a process-wide static that other Jumoo packages can add converters
///  to (JsonTextOptions.AddConverter), and converter order decides which one wins - so this is
///  a guard against a change somewhere else quietly re-formatting everyone's .config files.
/// </remarks>
[TestFixture]
internal class JsonSerializerOptionsTests
{
    [TestCase(typeof(object), "JsonObjectConverter")]
    [TestCase(typeof(JsonObject), "JsonObjectConverter")]
    [TestCase(typeof(JsonArray), "JsonArrayConverter")]
    [TestCase(typeof(JsonNode), "JsonNodeConverter")]
    [TestCase(typeof(string), "StringConverter")]
    [TestCase(typeof(bool), "JsonBooleanConverter")]
    [TestCase(typeof(int), "Int32Converter")]
    [TestCase(typeof(Guid), "GuidConverter")]
    [TestCase(typeof(Udi), "JsonUdiConverter")]
    [TestCase(typeof(GuidUdi), "JsonUdiConverter")]
    [TestCase(typeof(StringUdi), "JsonUdiConverter")]
    [TestCase(typeof(UdiRange), "JsonUdiRangeConverter")]
    [TestCase(typeof(XElement), "JsonXElementConverter")]
    [TestCase(typeof(BlockValue), "JsonBlockValueConverter")]
    [TestCase(typeof(BlockListValue), "JsonBlockValueConverter")]
    [TestCase(typeof(BlockGridValue), "JsonBlockValueConverter")]
    [TestCase(typeof(RichTextBlockValue), "JsonBlockValueConverter")]
    public void ConverterForType(Type type, string expectedConverter)
    {
        var converter = JsonTextOptions.GetOptions().GetConverter(type);

        Assert.That(converter?.GetType().Name, Is.EqualTo(expectedConverter));
    }

    [Test]
    public void PropertiesAreWrittenInNameOrder()
    {
        // ordered properties mean fewer changes in the physical files, which means faster
        // comparison checks - this is what OrderedPropertiesJsonResolver is for.
        var value = new OrderMe { Zebra = "z", Apple = "a", Mango = "m" };

        Assert.That(value.SerializeJsonString(false), Is.EqualTo("{\"apple\":\"a\",\"mango\":\"m\",\"zebra\":\"z\"}"));
    }

    [Test]
    public void FlatAndIndentedBothAvailable()
    {
        var value = new OrderMe { Apple = "a", Mango = "m", Zebra = "z" };

        Assert.Multiple(() =>
        {
            Assert.That(value.SerializeJsonString(false), Does.Not.Contain("\n"));
            Assert.That(value.SerializeJsonString(true), Does.Contain("\n"));
        });
    }

    private class OrderMe
    {
        public string Zebra { get; set; }
        public string Apple { get; set; }
        public string Mango { get; set; }
    }
}
