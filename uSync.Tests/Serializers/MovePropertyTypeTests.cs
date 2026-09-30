using System.Linq;

using NUnit.Framework;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;

using uSync.Core.Serialization.Serializers;

namespace uSync.Tests.Serializers;

/// <summary>
///  guards how the content type serializer moves properties between groups (#1009).
/// </summary>
/// <remarks>
///  before Umbraco 17.7, <c>MovePropertyType(alias, null)</c> orphaned the property,
///  and the serializer re-homes it. these tests assert the outcome, so they pass
///  whether or not core has the fix.
/// </remarks>
[TestFixture]
public class MovePropertyTypeTests
{
    private static IShortStringHelper ShortStringHelper
        => new DefaultShortStringHelper(new DefaultShortStringHelperConfig());

    private static IContentType BuildContentType()
    {
        var contentType = new ContentType(ShortStringHelper, -1)
        {
            Alias = "test",
            Name = "Test"
        };

        contentType.AddPropertyGroup("content", "Content");
        contentType.AddPropertyGroup("other", "Other");
        contentType.AddPropertyType(BuildPropertyType(), "content", "Content");

        return contentType;
    }

    private static PropertyType BuildPropertyType()
        => new(ShortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar)
        {
            Alias = "prop1",
            Name = "Prop1"
        };

    private static void Move(IContentType item, string alias, string group)
        => ContentTypeBaseSerializer<IContentType>.MovePropertyType(item, alias, group);

    [Test]
    public void MoveToNull_LandsInNoGroup()
    {
        var item = BuildContentType();

        Move(item, "prop1", null);

        Assert.Multiple(() =>
        {
            Assert.That(item.PropertyGroups["content"].PropertyTypes.Any(x => x.Alias == "prop1"), Is.False, "prop1 no longer in the group");
            Assert.That(item.PropertyTypes.Count(x => x.Alias == "prop1"), Is.EqualTo(1), "prop1 still exists on the content type");
            Assert.That(item.NoGroupPropertyTypes.Any(x => x.Alias == "prop1"), Is.True, "prop1 is in NoGroupPropertyTypes");
        });
    }

    [Test]
    public void MoveToNull_CaseInsensitiveAlias_LandsInNoGroup()
    {
        var item = BuildContentType();

        Move(item, "Prop1", null);

        Assert.That(item.PropertyTypes.Count(x => x.Alias == "prop1"), Is.EqualTo(1), "prop1 still exists on the content type");
    }

    [Test]
    public void MoveBetweenGroups_LandsInNewGroup()
    {
        var item = BuildContentType();

        Move(item, "prop1", "other");

        Assert.Multiple(() =>
        {
            Assert.That(item.PropertyGroups["content"].PropertyTypes.Any(x => x.Alias == "prop1"), Is.False, "prop1 no longer in the old group");
            Assert.That(item.PropertyGroups["other"].PropertyTypes.Any(x => x.Alias == "prop1"), Is.True, "prop1 is in the new group");
            Assert.That(item.PropertyTypes.Count(x => x.Alias == "prop1"), Is.EqualTo(1), "prop1 exists once");
        });
    }

    [Test]
    public void MoveFromNoGroup_LandsInGroup()
    {
        // before 17.7 the property also stays in the no-group collection until the
        // content type is reloaded, so that is not asserted here - see
        // MovePropertyTypePersistenceTests for what is saved.
        var item = BuildContentType();
        item.AddPropertyType(new PropertyType(ShortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar) { Alias = "prop2", Name = "Prop2" });

        Move(item, "prop2", "content");

        Assert.Multiple(() =>
        {
            Assert.That(item.PropertyGroups["content"].PropertyTypes.Any(x => x.Alias == "prop2"), Is.True, "prop2 is in the group");
            Assert.That(item.PropertyTypes.Count(x => x.Alias == "prop2"), Is.EqualTo(1), "prop2 exists once");
        });
    }
}
