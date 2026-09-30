using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.Core.Serialization.Serializers;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  property moves have to stick once the content type is saved and reloaded (#1009).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class MovePropertyTypePersistenceTests : UmbracoIntegrationTest
{
    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);
        configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Tests:Database:DatabaseType"] = "Sqlite",
            ["Tests:Database:PrepareThreadCount"] = "1",
            ["Tests:Database:SchemaDatabaseCount"] = "1",
            ["Tests:Database:EmptyDatabasesCount"] = "0"
        });
    }

    private IContentTypeService ContentTypeService => GetRequiredService<IContentTypeService>();

    private IContentType CreateContentType(string group)
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "test", Name = "Test" };
        contentType.AddPropertyGroup("content", "Content");

        var propertyType = new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "prop1")
        {
            Name = "Prop1",
            DataTypeId = UmbConstants.DataTypes.Textbox
        };

        if (group is null)
            contentType.AddPropertyType(propertyType);
        else
            contentType.AddPropertyType(propertyType, group, "Content");

        ContentTypeService.Save(contentType);
        return contentType;
    }

    private IContentType MoveAndReload(IContentType contentType, string group)
    {
        var item = ContentTypeService.Get(contentType.Key);
        ContentTypeBaseSerializer<IContentType>.MovePropertyType(item, "prop1", group);
        ContentTypeService.Save(item);
        return ContentTypeService.Get(contentType.Key);
    }

    [Test]
    public void MoveToNoGroup_IsSaved()
    {
        var reloaded = MoveAndReload(CreateContentType("content"), null);

        Assert.Multiple(() =>
        {
            Assert.That(reloaded.NoGroupPropertyTypes.Count(x => x.Alias == "prop1"), Is.EqualTo(1), "prop1 is in no group");
            Assert.That(reloaded.PropertyGroups["content"].PropertyTypes.Any(x => x.Alias == "prop1"), Is.False, "prop1 not in the group");
        });
    }

    [Test]
    public void MoveFromNoGroup_IsSaved()
    {
        var reloaded = MoveAndReload(CreateContentType(null), "content");

        Assert.Multiple(() =>
        {
            Assert.That(reloaded.PropertyGroups["content"].PropertyTypes.Count(x => x.Alias == "prop1"), Is.EqualTo(1), "prop1 is in the group");
            Assert.That(reloaded.NoGroupPropertyTypes.Any(x => x.Alias == "prop1"), Is.False, "prop1 not in no group");
        });
    }
}
