using System.Linq;

using NUnit.Framework;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;

namespace uSync.Tests.Serializers;

/// <summary>
///  guards the behaviour the content type serializer relies on when a property
///  is moved out of all groups (#1009).
/// </summary>
/// <remarks>
///  Umbraco's <c>MovePropertyType(alias, null)</c> removes the property from its
///  group but does NOT re-add it to the 'no group' collection - it orphans it
///  (see <see cref="MoveToNull_OrphansTheProperty"/>). The serializer works
///  around this by re-adding the property with <c>AddPropertyType</c>.
/// </remarks>
[TestFixture]
public class MovePropertyTypeTests
{
    private static IShortStringHelper ShortStringHelper
        => new DefaultShortStringHelper(new DefaultShortStringHelperConfig());

    private static IContentType BuildContentTypeWithGroupedProperty()
    {
        var contentType = new ContentType(ShortStringHelper, -1)
        {
            Alias = "test",
            Name = "Test"
        };

        var propertyType = new PropertyType(ShortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar)
        {
            Alias = "prop1",
            Name = "Prop1"
        };

        contentType.AddPropertyGroup("content", "Content");
        contentType.AddPropertyType(propertyType, "content", "Content");

        return contentType;
    }

    [Test]
    public void MoveToNull_OrphansTheProperty()
    {
        // documents the Umbraco behaviour the fix works around: moving a property
        // to a null group removes it from the group but loses it entirely.
        var item = BuildContentTypeWithGroupedProperty();

        item.MovePropertyType("prop1", null);

        Assert.Multiple(() =>
        {
            Assert.That(item.PropertyGroups["content"].PropertyTypes.Any(x => x.Alias == "prop1"), Is.False, "removed from group");
            Assert.That(item.PropertyTypes.Any(x => x.Alias == "prop1"), Is.False, "but also orphaned from the content type");
        });
    }

    [Test]
    public void MoveToNull_ThenReAdd_LandsInNoGroup()
    {
        // the approach the serializer uses: move out, then re-home into no-group.
        var item = BuildContentTypeWithGroupedProperty();

        var property = item.PropertyTypes.FirstOrDefault(x => x.Alias == "prop1");
        item.MovePropertyType("prop1", null);
        if (property is not null && item.PropertyTypes.Any(x => x.Alias == "prop1") is false)
            item.AddPropertyType(property);

        Assert.Multiple(() =>
        {
            Assert.That(item.PropertyGroups["content"].PropertyTypes.Any(x => x.Alias == "prop1"), Is.False, "prop1 no longer in the group");
            Assert.That(item.PropertyTypes.Any(x => x.Alias == "prop1"), Is.True, "prop1 still exists on the content type");
            Assert.That(item.NoGroupPropertyTypes.Any(x => x.Alias == "prop1"), Is.True, "prop1 is in NoGroupPropertyTypes");
        });
    }
}
