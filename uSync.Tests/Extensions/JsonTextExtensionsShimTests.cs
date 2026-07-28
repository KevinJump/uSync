using NUnit.Framework;

using System.Text.Json.Nodes;

using uSync.Core.Extensions;

namespace uSync.Tests.Extensions;

/// <summary>
///  The obsolete JsonTextExtensions forwarders are what downstream packages (uSync.Complete,
///  community mappers) still compile against, so their contracts have to keep working exactly
///  as they did before the move to Jumoo.Json.
/// </summary>
/// <remarks>
///  This file deliberately imports uSync.Core.Extensions and *not* Jumoo.Json - it is testing
///  the shim, not the library underneath it. The two can't both be in scope here.
/// </remarks>
#pragma warning disable CS0618 // testing the obsolete API on purpose
[TestFixture]
internal class JsonTextExtensionsShimTests
{
    [Test]
    public void SerializeJsonString_ReturnsEmptyString_ForNull()
    {
        object value = null;

        Assert.That(value.SerializeJsonString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetPropertyAsString_ReturnsEmptyString_ForMissingProperty()
    {
        var jsonObject = "{ \"name\": \"Test\" }".ToJsonObject();

        Assert.That(jsonObject.GetPropertyAsString("missing"), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetPropertyAsString_ReturnsTheValue_ForPresentProperty()
    {
        var jsonObject = "{ \"name\": \"Test\" }".ToJsonObject();

        Assert.That(jsonObject.GetPropertyAsString("name"), Is.EqualTo("Test"));
    }

    [Test]
    public void ToJsonArray_ReturnsNull_ForSomethingThatIsNotAnArray()
    {
        Assert.That("not json".ToJsonArray(), Is.Null);
    }

    [Test]
    public void SerializeJsonNode_ReturnsAString()
    {
        var node = "{ \"name\": \"Test\" }".ToJsonNode();

        Assert.That(node.SerializeJsonNode(false), Is.EqualTo("{\"name\":\"Test\"}"));
    }

    [Test]
    public void GetPropertyAsBool_ReturnsTheDefault_ForMissingProperty()
    {
        var jsonObject = "{ \"name\": \"Test\" }".ToJsonObject();

        Assert.That(jsonObject.GetPropertyAsBool("missing", true), Is.True);
    }

    [Test]
    public void TryGetPropertyAsObject_KeepsTheOldName()
    {
        var jsonObject = "{ \"item\": { \"name\": \"Test\" } }".ToJsonObject();

        var result = jsonObject.TryGetPropertyAsObject("item", out JsonObject item);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(item.GetPropertyAsString("name"), Is.EqualTo("Test"));
        });
    }

    [Test]
    public void GetPropertyAsObject_KeepsTheOldName()
    {
        var jsonObject = "{ \"item\": { \"name\": \"Test\" } }".ToJsonObject();

        Assert.That(jsonObject.GetPropertyAsObject("item")?.GetPropertyAsString("name"), Is.EqualTo("Test"));
    }

    [TestCase("He said \"hello\"")]
    [TestCase("c:\\temp\\file.txt")]
    public void TryConvertToJsonNode_HandlesStringsThatNeedEscaping(string value)
    {
        // this used to fail - the fallback was built with JsonNode.Parse($"\"{value}\""),
        // which is invalid json as soon as the value contains a quote or a backslash.
        var result = value.TryConvertToJsonNode(out JsonNode node);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(node?.ToString(), Is.EqualTo(value));
        });
    }
}
#pragma warning restore CS0618
