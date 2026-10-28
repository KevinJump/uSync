using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.Core.Serialization;
using uSyncConstants = uSync.Core.uSyncConstants;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  content is saved and published as a user, so Umbraco's checks for that
///  user apply (v19: invariant-for-variant, and the user has to exist).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentImportUserTests : UmbracoIntegrationTest
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

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        builder.AdduSync(settings =>
        {
            settings.ExportAtStartup = "None";
            settings.ExportOnSave = "None";
            settings.ImportAtStartup = "None";
            settings.BackgroundStartup = false;
        });
    }

    private ISyncSerializer<IContent> Serializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>("ContentSerializer");

    private IContentService ContentService => GetRequiredService<IContentService>();

    private async Task<IContent> CreatePublishedContent()
    {
        await GetRequiredService<ILanguageService>().CreateAsync(new Language("da-DK", "Danish"), UmbConstants.Security.SuperUserKey);

        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1)
        {
            Alias = "variantPage",
            Name = "Variant Page",
            Variations = ContentVariation.Culture
        };
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            Variations = ContentVariation.Culture,
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "plain")
        {
            Name = "Plain",
            Variations = ContentVariation.Nothing,
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, UmbConstants.Security.SuperUserKey);

        var content = ContentService.Create("page", -1, contentType);
        foreach (var culture in new[] { "en-US", "da-DK" })
        {
            content.SetCultureName($"page {culture}", culture);
            content.SetValue("title", $"title {culture}", culture);
        }
        content.SetValue("plain", "original");
        ContentService.Save(content);
        ContentService.Publish(content, ["en-US", "da-DK"]);

        return ContentService.GetById(content.Key)!;
    }

    private async Task<IUser> CreateUser(bool hasAccessToInvariantForVariant)
    {
        var userService = GetRequiredService<IUserService>();

        var user = UserBuilder.CreateUser();
        userService.Save(user);

        var group = new UserGroupBuilder().Build();
        group.HasAccessToInvariantForVariant = hasAccessToInvariantForVariant;

        var result = await GetRequiredService<IUserGroupService>().CreateAsync(group, UmbConstants.Security.SuperUserKey, [user.Key]);
        Assert.That(result.Success, Is.True);

        return userService.GetUserById(user.Id)!;
    }

    private static void SetPlainValue(XElement node, string value)
        => node.Element("Properties")!.Element("plain")!.Element("Value")!.ReplaceNodes(new XCData(value));

    // user info is only in the file when IncludeUserInfo is on.
    private static void SetWriter(XElement node, string email)
        => node.Element(uSyncConstants.Xml.Info)!.Add(new XElement("UserInfo", new XElement("Writer", email)));

    [TestCase(true, "changed")]
    [TestCase(false, "original")]
    public async Task Publish_AppliesTheImportingUsersInvariantForVariantPermission(bool hasAccess, string expectedPublished)
    {
        var content = await CreatePublishedContent();
        var user = await CreateUser(hasAccess);

        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;
        SetPlainValue(node, "changed");

        var options = new SyncSerializerOptions(SerializerFlags.Force, [], user.Id);
        var attempt = await Serializer.DeserializeAsync(node, options);
        var reloaded = ContentService.GetById(content.Key)!;

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(reloaded.GetValue("plain"), Is.EqualTo("changed"));
            Assert.That(reloaded.GetValue("plain", published: true), Is.EqualTo(expectedPublished));
            Assert.That(reloaded.PublisherId, Is.EqualTo(user.Id));
        });
    }

    [Test]
    public async Task FileWriter_WinsOverTheImportingUser()
    {
        var content = await CreatePublishedContent();
        var writer = await CreateUser(hasAccessToInvariantForVariant: false);

        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;
        SetPlainValue(node, "changed");
        SetWriter(node, writer.Email);

        var attempt = await Serializer.DeserializeAsync(node,
            new SyncSerializerOptions(SerializerFlags.Force, [], uSyncConstants.SuperUserId));
        var reloaded = ContentService.GetById(content.Key)!;

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(reloaded.WriterId, Is.EqualTo(writer.Id));
            Assert.That(reloaded.GetValue("plain", published: true), Is.EqualTo("original"), "published as the writer, who can't publish invariant values");
        });
    }

    [Test]
    public async Task UnknownWriter_UsesTheImportingUser()
    {
        var content = await CreatePublishedContent();
        var user = await CreateUser(hasAccessToInvariantForVariant: true);

        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;
        SetWriter(node, "nobody@example.com");
        SetPlainValue(node, "changed");

        var attempt = await Serializer.DeserializeAsync(node, new SyncSerializerOptions(SerializerFlags.Force, [], user.Id));

        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(ContentService.GetById(content.Key)!.WriterId, Is.EqualTo(user.Id));
        });
    }

    [Test]
    public void UnknownUserId_FallsBackToTheSuperuser()
    {
        var options = new SyncSerializerOptions(SerializerFlags.None, [], UmbConstants.Security.UnknownUserId);
        Assert.That(options.UserId, Is.EqualTo(uSyncConstants.SuperUserId));
    }
}
