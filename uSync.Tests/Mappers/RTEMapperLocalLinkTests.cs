using System;
using System.Linq;

using NUnit.Framework;

using uSync.Core.Mapping;

namespace uSync.Tests.Mappers;

/// <summary>
///  checks the localLink regex picks up links in both the plain and url encoded
///  forms, Umbraco resolves %7BlocalLink:guid%7D from v17.8 (umbraco/Umbraco-CMS#23767)
/// </summary>
[TestFixture]
public class RTEMapperLocalLinkTests
{
    private const string _key = "9b0c5f4e-3c1a-4d2b-8e7f-1a2b3c4d5e6f";

    [TestCase($"<a href=\"/{{localLink:{_key}}}\">link</a>", TestName = "Plain braces")]
    [TestCase($"<a href=\"/%7BlocalLink:{_key}%7D\">link</a>", TestName = "Encoded braces")]
    [TestCase($"<a href=\"/%7blocallink:{_key}%7d\">link</a>", TestName = "Encoded lowercase")]
    [TestCase($"<a href=\"/{{localLink:{_key}%7D\">link</a>", TestName = "Mixed braces")]
    public void LocalLink_Guid_IsFound(string markup)
    {
        var guids = RTEMapper.GetLocalLinkGuids(RTEMapper.LinkRegExPattern(), markup).ToList();

        Assert.That(guids, Is.EqualTo(new[] { Guid.Parse(_key) }));
    }

    [Test]
    public void Neighbouring_LocalLinks_AreAllFound()
    {
        var other = Guid.NewGuid();
        var markup = $"<a href=\"/%7BlocalLink:{_key}%7D\">one</a><a href=\"/{{localLink:{other}}}\">two</a>";

        var guids = RTEMapper.GetLocalLinkGuids(RTEMapper.LinkRegExPattern(), markup).ToList();

        Assert.That(guids, Is.EqualTo(new[] { Guid.Parse(_key), other }));
    }

    [TestCase($"<a href=\"/localLink:{_key}\">link</a>", TestName = "No braces")]
    [TestCase("<a href=\"/%7BlocalLink:not-a-guid%7D\">link</a>", TestName = "Not a guid")]
    public void Invalid_LocalLink_IsIgnored(string markup)
    {
        var guids = RTEMapper.GetLocalLinkGuids(RTEMapper.LinkRegExPattern(), markup);

        Assert.That(guids, Is.Empty);
    }
}
