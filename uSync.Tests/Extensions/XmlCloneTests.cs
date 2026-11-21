using NUnit.Framework;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

using uSync.Core;

namespace uSync.Tests.Extensions;

/// <summary>
///  CloneWithoutWhitespace replaces XElement.Parse(node.ToString()) as a deep copy,
///  so it must give the same xml (and the same hash) the round trip did.
/// </summary>
[TestFixture]
public class XmlCloneTests
{
    private static XElement RoundTrip(XElement node) => XElement.Parse(node.ToString());

    private static IEnumerable<TestCaseData> Nodes()
    {
        yield return new TestCaseData(XElement.Parse(
            "<Item Key=\"1\" Alias=\"a\"><Info><Name>Name</Name><Parent Key=\"2\">p</Parent></Info><Empty /></Item>"))
            .SetName("Simple");

        // whitespace only values, the parser drops these.
        yield return new TestCaseData(new XElement("Item",
            new XElement("Space", " "),
            new XElement("Tab", "\t"),
            new XElement("Blank", string.Empty),
            new XElement("SelfClosed")))
            .SetName("WhitespaceValues");

        // whitespace text between elements (as you get from a PreserveWhitespace load).
        yield return new TestCaseData(XElement.Parse(
            "<Item>\r\n  <A>1</A>\r\n  <B>\r\n    <C>2</C>\r\n  </B>\r\n</Item>", LoadOptions.PreserveWhitespace))
            .SetName("IndentedWhitespace");

        // cdata (even whitespace cdata) is kept.
        yield return new TestCaseData(new XElement("Item",
            new XElement("Value", new XCData(" ")),
            new XElement("Json", new XCData("{ \"a\" : 1 }"))))
            .SetName("CData");

        // mixed content and characters that need escaping.
        yield return new TestCaseData(XElement.Parse(
            "<Item><Mixed>text <b>bold</b> more </Mixed><Esc a=\"&lt;&amp;&quot;\">&lt;tag&gt; &amp;</Esc><Nbsp> </Nbsp></Item>",
            LoadOptions.PreserveWhitespace))
            .SetName("MixedAndEscaped");

        yield return new TestCaseData(XElement.Parse(
            "<Item><Multi>line one\nline two</Multi><Attr v=\"a&#xA;b\" /></Item>"))
            .SetName("NewLines");
    }

    [TestCaseSource(nameof(Nodes))]
    public void Clone_Matches_RoundTrip(XElement node)
    {
        var expected = RoundTrip(node);
        var clone = node.CloneWithoutWhitespace();

        Assert.That(clone.ToString(), Is.EqualTo(expected.ToString()));
        Assert.That(clone.ToString(SaveOptions.DisableFormatting), Is.EqualTo(expected.ToString(SaveOptions.DisableFormatting)));
    }

    [TestCaseSource(nameof(Nodes))]
    public async Task Clone_Hash_Matches_RoundTrip(XElement node)
    {
        var expected = await RoundTrip(node).MakePlatformSafeHashAsync();
        var actual = await node.CloneWithoutWhitespace().MakePlatformSafeHashAsync();

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Clone_DoesNotChange_Source()
    {
        var node = new XElement("Item", new XElement("Space", " "));
        var before = node.ToString(SaveOptions.DisableFormatting);

        var clone = node.CloneWithoutWhitespace();
        clone.Add(new XElement("Extra"));

        Assert.That(node.ToString(SaveOptions.DisableFormatting), Is.EqualTo(before));
    }

    [Test]
    public void IsSameXml_Matches_StringCompare()
    {
        var a = XElement.Parse("<Item Key=\"1\"><Name>A</Name></Item>");
        var b = XElement.Parse("<Item Key=\"1\"><Name>A</Name></Item>");
        var c = XElement.Parse("<Item Key=\"1\"><Name>B</Name></Item>");

        Assert.That(a.IsSameXml(b), Is.True);
        Assert.That(a.IsSameXml(c), Is.False);
    }
}
