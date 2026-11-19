using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.Core;
using uSync.Core.Serialization;
using uSync.Tests.Extensions;

using UmbConstants = Umbraco.Cms.Core.Constants;

namespace uSync.Tests.Serializers;

/// <summary>
///  schedule dates in a uSync file are UTC, so importing them must not depend on the server's local time.
/// </summary>
[TestFixture]
[NonParallelizable]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentScheduleImportTests : UmbracoIntegrationTest
{
    private TimeZoneInfo _originalZone = null!;

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

    [SetUp]
    public void CaptureTimeZone() => _originalZone = TimeZoneInfo.Local;

    [TearDown]
    public void RestoreTimeZone()
    {
        TestTimeZone.Restore();
        Assert.That(TimeZoneInfo.Local.Id, Is.EqualTo(_originalZone.Id));
        Assert.That(TimeZoneInfo.Local.BaseUtcOffset, Is.EqualTo(_originalZone.BaseUtcOffset));
    }

    private ISyncSerializer<IContent> Serializer
        => GetRequiredService<SyncSerializerCollection>().GetSerializer<IContent>("ContentSerializer");

    private async Task<IContent> CreateContent()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1)
        {
            Alias = "schedulePage",
            Name = "Schedule Page"
        };
        contentType.AddPropertyType(new PropertyType(shortStringHelper, "Umbraco.TextBox", ValueStorageType.Nvarchar, "title")
        {
            Name = "Title",
            DataTypeId = UmbConstants.DataTypes.Textbox
        });
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, UmbConstants.Security.SuperUserKey);

        var contentService = GetRequiredService<IContentService>();
        var content = contentService.Create("page", -1, contentType);
        content.SetValue("title", "title");
        contentService.Save(content);
        return content;
    }

    [Test]
    public async Task Import_FutureRelease_LocalAheadOfUtc_KeepsSchedule()
    {
        var content = await CreateContent();
        var node = (await Serializer.SerializeAsync(content, new SyncSerializerOptions())).Item;

        node.Element(Core.uSyncConstants.Xml.Info)!.Element("Schedule")!.ReplaceWith(
            new XElement("Schedule",
                new XElement("ContentSchedule",
                    new XElement("Culture", string.Empty),
                    new XElement("Action", ContentScheduleAction.Release),
                    new XElement("Date", DateTime.UtcNow.AddHours(2).ToString("s")))));

        TestTimeZone.SetLocal(TimeSpan.FromHours(11));

        var attempt = await Serializer.DeserializeAsync(node,
            new SyncSerializerOptions(SerializerFlags.Force | SerializerFlags.OnePass));

        var schedules = GetRequiredService<IContentService>().GetContentScheduleByContentId(content.Id);
        Assert.Multiple(() =>
        {
            Assert.That(attempt.Success, Is.True);
            Assert.That(schedules.FullSchedule.Select(x => x.Action), Has.Member(ContentScheduleAction.Release));
        });
    }
}
