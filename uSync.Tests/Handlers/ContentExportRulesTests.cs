using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Handlers;

namespace uSync.Tests.Handlers;

/// <summary>
///  Include and Exclude paths on an export all, with RulesOnExport on, only
///  export (and only walk) the parts of the tree they cover (#563).
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentExportRulesTests : UmbracoIntegrationTest
{
    private const string _root = "uSync/tests/export-rules";

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

    [TearDown]
    public void RemoveExports()
        => GetRequiredService<ISyncFileService>().DeleteFolder(_root);

    private ContentHandler Handler
        => GetRequiredService<SyncHandlerCollection>().OfType<ContentHandler>().Single();

    /// <summary>
    ///  /home/products/widget, /home/about/team and /other/page
    /// </summary>
    private async Task<Dictionary<string, Guid>> CreateTree()
    {
        var shortStringHelper = GetRequiredService<IShortStringHelper>();
        var contentType = new ContentType(shortStringHelper, -1) { Alias = "page", Name = "Page", AllowedAsRoot = true };
        await GetRequiredService<IContentTypeService>().CreateAsync(contentType, Umbraco.Cms.Core.Constants.Security.SuperUserKey);

        var contentService = GetRequiredService<IContentService>();
        var keys = new Dictionary<string, Guid>();

        IContent Add(string name, IContent? parent)
        {
            var content = contentService.Create(name, parent?.Id ?? -1, contentType);
            contentService.Save(content);
            keys[name] = content.Key;
            return content;
        }

        var home = Add("home", null);
        Add("widget", Add("products", home));
        Add("team", Add("about", home));
        Add("page", Add("other", null));

        return keys;
    }

    private HandlerSettings Config(string? include = null, string? exclude = null, bool rulesOnExport = true)
    {
        var config = Handler.DefaultConfig.Clone();
        config.Settings["RulesOnExport"] = rulesOnExport.ToString();
        if (include is not null) config.Settings["Include"] = include;
        if (exclude is not null) config.Settings["Exclude"] = exclude;
        return config;
    }

    private async Task<string[]> ExportAll(Dictionary<string, Guid> keys, HandlerSettings config)
    {
        var actions = await Handler.ExportAllAsync([$"{_root}/{Guid.NewGuid():N}/"], config, null);
        var names = keys.ToDictionary(x => x.Value, x => x.Key);
        return [.. actions.Select(x => names[x.Key]).Order()];
    }

    [Test]
    public async Task ExportAll_Include_OnlyVisitsIncludedBranch()
    {
        var keys = await CreateTree();

        var exported = await ExportAll(keys, Config(include: "/home/products"));

        Assert.That(exported, Is.EqualTo(new[] { "products", "widget" }));
    }

    [Test]
    public async Task ExportAll_Exclude_SkipsExcludedBranch()
    {
        var keys = await CreateTree();

        var exported = await ExportAll(keys, Config(exclude: "/home/about"));

        Assert.That(exported, Is.EqualTo(new[] { "home", "other", "page", "products", "widget" }));
    }

    [Test]
    public async Task ExportAll_IncludeWithoutRulesOnExport_ExportsEverything()
    {
        var keys = await CreateTree();

        var exported = await ExportAll(keys, Config(include: "/home/products", rulesOnExport: false));

        Assert.That(exported, Is.EqualTo(keys.Keys.Order().ToArray()));
    }
}
