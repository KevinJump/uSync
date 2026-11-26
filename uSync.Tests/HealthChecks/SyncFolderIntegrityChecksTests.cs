using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.HealthChecks;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.HealthChecks;
using uSync.BackOffice.Services;

namespace uSync.Tests.HealthChecks;

[TestFixture]
public class SyncFolderIntegrityChecksTests
{
    private string _root;

    [SetUp]
    public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "uSyncIntegrityTests", Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_root, true); } catch { /* temp folder */ }
    }

    private SyncFolderIntegrityChecks CreateCheck()
    {
        var config = new Mock<ISyncConfigService>();
        config.Setup(x => x.GetWorkingFolder()).Returns(_root);

        var files = new Mock<ISyncFileService>();
        files.Setup(x => x.GetAbsPath(It.IsAny<string>())).Returns<string>(x => x);
        files.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(Directory.Exists);

        return new SyncFolderIntegrityChecks(config.Object, files.Object);
    }

    private void Write(string path, string content)
    {
        var file = Path.Combine(_root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file));
        File.WriteAllText(file, content);
    }

    private static string ContentType(Guid key, string alias)
        => $"<ContentType Key=\"{key}\" Alias=\"{alias}\" Level=\"1\"><Info><Name>{alias}</Name></Info></ContentType>";

    private async Task<List<HealthCheckStatus>> Run()
        => (await CreateCheck().GetStatusAsync()).ToList();

    [Test]
    public async Task Clean_Folder_Is_All_Success()
    {
        Write("ContentTypes/article.config", ContentType(Guid.NewGuid(), "article"));
        Write("ContentTypes/home.config", ContentType(Guid.NewGuid(), "home"));

        var results = await Run();

        Assert.That(results, Has.Count.EqualTo(5));
        Assert.That(results, Has.All.Property(nameof(HealthCheckStatus.ResultType)).EqualTo(StatusResultType.Success));
        Assert.That(results[0].Message, Is.EqualTo("No Id clashes found"));
        Assert.That(results[1].Message, Is.EqualTo("No File errors"));
    }

    [Test]
    public async Task Missing_Folder_Is_Success()
    {
        Directory.Delete(_root, true);

        var results = await Run();

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].ResultType, Is.EqualTo(StatusResultType.Success));
    }

    [Test]
    public async Task Duplicate_Keys_Are_Errors()
    {
        var key = Guid.NewGuid();
        Write("ContentTypes/article.config", ContentType(key, "article"));
        Write("ContentTypes/article_copy.config", ContentType(key, "articleCopy"));

        var results = await Run();

        Assert.That(results[0].ResultType, Is.EqualTo(StatusResultType.Error));
        Assert.That(results[0].Message, Does.Contain("share the same keys"));
        Assert.That(results[0].Description, Does.Contain("ContentTypes/article_copy.config"));
    }

    [Test]
    public async Task Same_Key_In_Different_Types_Is_Not_A_Clash()
    {
        // e.g. PublicAccess and Content share the content node's key.
        var key = Guid.NewGuid();
        Write("Content/home.config", $"<Content Key=\"{key}\" Alias=\"Home\"><Info /></Content>");
        Write("PublicAccess/home.config", $"<PublicAccess Key=\"{key}\" Alias=\"Home\" />");

        var results = await Run();

        Assert.That(results[0].ResultType, Is.EqualTo(StatusResultType.Success));
    }

    [Test]
    public async Task Invalid_Xml_Is_An_Error()
    {
        Write("ContentTypes/broken.config", "<ContentType Key=\"x\"");

        var results = await Run();

        Assert.That(results[1].ResultType, Is.EqualTo(StatusResultType.Error));
        Assert.That(results[1].Description, Does.Contain("ContentTypes/broken.config"));
    }

    [Test]
    public async Task Conflict_Markers_In_A_Value_Are_An_Error()
    {
        Write("DataTypes/blocks.config",
            "<DataType Key=\"" + Guid.NewGuid() + "\" Alias=\"blocks\">\n<Config><![CDATA[\n<<<<<<< HEAD\n{ \"a\": 1 }\n=======\n{ \"a\": 2 }\n>>>>>>> branch\n]]></Config>\n</DataType>");

        var results = await Run();

        var conflicts = results[2];
        Assert.That(conflicts.ResultType, Is.EqualTo(StatusResultType.Error));
        Assert.That(conflicts.Description, Does.Contain("DataTypes/blocks.config:3"));
        Assert.That(results[1].ResultType, Is.EqualTo(StatusResultType.Success), "still valid xml");
    }

    [Test]
    public async Task Alias_Clash_Is_An_Error()
    {
        Write("ContentTypes/article.config", ContentType(Guid.NewGuid(), "article"));
        Write("ContentTypes/article_1.config", ContentType(Guid.NewGuid(), "article"));

        var results = await Run();

        Assert.That(results[3].ResultType, Is.EqualTo(StatusResultType.Error));
        Assert.That(results[3].Message, Does.Contain("2 item(s)"));
    }

    [Test]
    public async Task Deleted_And_Live_Is_An_Error()
    {
        var key = Guid.NewGuid();
        Write("ContentTypes/article.config", ContentType(key, "article"));
        Write("ContentTypes/article_deleted.config", $"<Empty Key=\"{key}\" Alias=\"article\" Change=\"Delete\" />");

        var results = await Run();

        Assert.That(results[4].ResultType, Is.EqualTo(StatusResultType.Error));
        Assert.That(results[0].ResultType, Is.EqualTo(StatusResultType.Success));
    }
}
