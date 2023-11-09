using Lucene.Net.Documents;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using uSync.Core;

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
        var result = value.GetJTokenFromObject();

        Assert.IsNotNull(result);
        Assert.IsInstanceOf<JToken>(result);
    }

    [TestCase("[SQUARE]")]
    [TestCase("{\"One\", \"Two\"}")]
    public void BadJsonReturnsNull(object value)
    {
        var result = value.GetJTokenFromObject();
        Assert.IsNull(result);
    }

    [TestCase("{}")]
    [TestCase("[]")]
    [TestCase("{ \"name\": \"Test\" }")]
    [TestCase("[1,2,3]")]
    [TestCase("[\"one\",\"two\",\"three\"]")]
    public void JTokenValuesCanBeParsed(string value)
    {
        var result = value.TryParseValidJsonString(out JToken token);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<JToken>(token);
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
        var result = value.TryParseValidJsonString(out JToken token);

        Assert.IsFalse(result);
        Assert.IsNull(token);
    }

    [TestCase("[]", 0)]
    [TestCase("[1,2,3]", 3)]
    [TestCase("[\"one\",\"two\",\"three\"]", 3)]
    public void JArrayValuesCanBeParsed(string value, int expectedLength)
    {
        var result = value.TryParseValidJsonString<JArray>(out JArray array);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<JArray>(array);
        Assert.AreEqual(array.Count, expectedLength);
    }

    [TestCase("{ \"name\": \"Test\" }")]
    [TestCase("{ \"Age\": 30 }")]
    [TestCase("{\r\n\"employee\":{\"name\":\"John\", \"age\":30, \"city\":\"New York\"}\r\n}")]
    [TestCase("{\"middlename\":null}\r\n")]
    public void JObjectValuesCanBeParsed(string value)
    {
        var result = value.TryParseValidJsonString<JObject>(out JObject obj);

        Assert.IsTrue(result);
        Assert.IsInstanceOf<JObject>(obj);
    }

}
