using System.Text.Json.Nodes;


using NUnit.Framework;

using uSync.Core.Extensions;

namespace uSync.Tests.Extensions;

[TestFixture]
internal class JsonDetectionTests
{
    [TestCase("String value")]
    [TestCase("12")]
    [TestCase("\"Quoted string\"")]
    [TestCase("20230101T12:00")]
    [TestCase("")]
    // DetectJson will return true, but its not json
    [TestCase("[SQUARE]")]
    [TestCase("{{angular-variable}}")]
    [TestCase("{{angular | filter : value}}")]
    [TestCase("{thing}")]
    public void StringValuesAreStrings(string value)
    {
        var result = value.IsValidJsonString();
        Assert.IsFalse(result);
    }

    [TestCase("{ \"name\": \"Test\" }")]
    [TestCase("{ \"Age\": 30 }")]
    [TestCase("{\r\n\"employee\":{\"name\":\"John\", \"age\":30, \"city\":\"New York\"}\r\n}")]
    [TestCase("{\"middlename\":null}\r\n")]
    [TestCase("[1,2,3]")]
    [TestCase("[\"one\",\"two\",\"three\"]")]
    public void JsonValueIsJson(string value)
    {
        var result = value.IsValidJsonString();
        Assert.IsTrue(result);
    }

    [TestCase("[]")]
    [TestCase("{}")]
    public void EmptyJsonIsJson(string value)
    {
        var result = value.IsValidJsonString();
        Assert.IsTrue(result);
    }

    [TestCase("{}")]
    [TestCase("")]
    [TestCase("[]")]
    [TestCase("{ \"name\": \"Test\" }")]
    [TestCase("[1,2,3]")]
    [TestCase("[\"one\",\"two\",\"three\"]")]
    public void CanBeCastToJToken(object value)
    {
        value.TryConvertToJsonNode(out var result);

        Assert.IsNotNull(result);
        Assert.IsInstanceOf<JsonNode>(result);
    }

    [TestCase("[SQUARE]")]
    [TestCase("{\"One\", \"Two\"}")]
    public void BadJsonReturnsNull(object value)
    {
        value.TryParseToJsonNode(out var result);
        Assert.IsNull(result);
    }

    [TestCase("{}")]
    [TestCase("[]")]
    [TestCase("{ \"name\": \"Test\" }")]
    [TestCase("[1,2,3]")]
    [TestCase("[\"one\",\"two\",\"three\"]")]
    public void JTokenValuesCanBeParsed(string value)
    {
        var result = value.TryParseToJsonNode(out JsonNode node);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<JsonNode>(node);
    }

    [TestCase("Hello")]
    [TestCase("")]
    [TestCase("[SQUARE]")]
    [TestCase("{{angular-variable}}")]
    [TestCase("{{angular | filter : value}}")]
    [TestCase("{thing}")]
    [TestCase(null)]
    public void StringIsNotParsed(string value)
    {
        var result = value.TryParseToJsonNode(out JsonNode node);

        Assert.IsFalse(result);
        Assert.IsNull(node);
    }

    [TestCase("[]", 0)]
    [TestCase("[1,2,3]", 3)]
    [TestCase("[\"one\",\"two\",\"three\"]", 3)]
    public void JArrayValuesCanBeParsed(string value, int expectedLength)
    {
        var result = value.TryParseToJsonArray(out JsonArray array);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<JsonArray>(array);
        Assert.AreEqual(array.Count, expectedLength);
    }

    [TestCase("{ \"name\": \"Test\" }")]
    [TestCase("{ \"Age\": 30 }")]
    [TestCase("{\r\n\"employee\":{\"name\":\"John\", \"age\":30, \"city\":\"New York\"}\r\n}")]
    [TestCase("{\"middlename\":null}\r\n")]
    public void JObjectValuesCanBeParsed(string value)
    {
        var result = value.TryParseToJsonObject(out JsonObject obj);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<JsonObject>(obj);
    }

}
