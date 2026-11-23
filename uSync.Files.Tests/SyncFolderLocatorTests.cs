using uSync.Files;

namespace uSync.Files.Tests;

/// <summary>
///  folder discovery and the publish folder check, called directly
///  (the command line covers the same ground through files-verify).
/// </summary>
public class SyncFolderLocatorTests
{
    private readonly List<string> _temp = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var folder in _temp) Fixtures.DeleteFolder(folder);
        _temp.Clear();
    }

    private string Temp(string? fixture = null)
    {
        var folder = fixture is null ? Fixtures.TempFolder() : Fixtures.CopyToTemp(fixture);
        _temp.Add(folder);
        return folder;
    }

    [Test]
    public void Discovers_Folder_From_AppSettings()
    {
        var site = Temp("clean");
        Directory.Move(Path.Combine(site, "uSync", "v17"), Path.Combine(site, "uSync", "custom"));
        File.WriteAllText(Path.Combine(site, "appsettings.json"),
            """
            {
              // comments are allowed
              "uSync": { "Settings": { "RootFolder": "~/uSync/custom/" } }
            }
            """);

        var location = SyncFolderLocator.Discover(site);
        Assert.That(location.Folder, Is.EqualTo(Path.Combine(site, "uSync", "custom")));
        Assert.That(location.SiteRoot, Is.EqualTo(site));
    }

    [Test]
    public void Discovers_Last_Of_Folders_From_AppSettings()
    {
        var site = Temp("clean");
        Directory.CreateDirectory(Path.Combine(site, "uSync", "Root"));
        File.WriteAllText(Path.Combine(site, "appsettings.json"),
            """{ "uSync": { "Settings": { "Folders": [ "uSync/Root/", "uSync/v17/" ] } } }""");

        Assert.That(SyncFolderLocator.Discover(site).Folder, Is.EqualTo(Path.Combine(site, "uSync", "v17")));
    }

    [Test]
    public void Discovers_Highest_Version_Folder_In_A_Sub_Folder()
    {
        var repo = Temp();
        var site = Path.Combine(repo, "src", "MySite");
        Fixtures.CopyDirectory(Fixtures.Site("clean"), site);
        Directory.CreateDirectory(Path.Combine(site, "uSync", "v9"));
        Directory.CreateDirectory(Path.Combine(site, "uSync", "v16"));

        var location = SyncFolderLocator.Discover(repo);
        Assert.That(location.Folder, Is.EqualTo(Path.Combine(site, "uSync", "v17")));
        Assert.That(location.SiteRoot, Is.EqualTo(site));
    }

    [Test]
    public void Two_Sites_Throws()
    {
        var repo = Temp();
        Fixtures.CopyDirectory(Fixtures.Site("clean"), Path.Combine(repo, "SiteA"));
        Fixtures.CopyDirectory(Fixtures.Site("clean"), Path.Combine(repo, "SiteB"));

        Assert.Throws<SyncFolderNotFoundException>(() => SyncFolderLocator.Discover(repo));
    }

    [Test]
    public void Publish_Folder_Missing_uSync_Is_An_Error()
    {
        var site = Temp("clean");
        var publish = Path.Combine(site, "publish");
        Directory.CreateDirectory(publish);

        var issues = SyncPublishFolderCheck.Check(Path.Combine(site, "uSync", "v17"), site, publish);
        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Rule, Is.EqualTo(SyncFileRules.PublishFolder));
        Assert.That(issues[0].Message, Does.Contain("no 'uSync/v17' folder"));
    }

    [Test]
    public void Publish_Folder_With_Missing_And_Changed_Files_Is_An_Error()
    {
        var site = Temp("clean");
        var publish = Path.Combine(site, "publish");
        Fixtures.CopyDirectory(Path.Combine(site, "uSync"), Path.Combine(publish, "uSync"));
        File.Delete(Path.Combine(publish, "uSync", "v17", "Templates", "home.config"));
        File.AppendAllText(Path.Combine(publish, "uSync", "v17", "DataTypes", "Textstring.config"), " ");

        var issues = SyncPublishFolderCheck.Check(Path.Combine(site, "uSync", "v17"), site, publish);
        Assert.That(issues, Has.Count.EqualTo(2));
        Assert.That(issues, Has.One.Property(nameof(SyncFileIssue.Message)).Contains("Templates/home.config' is missing"));
        Assert.That(issues, Has.One.Property(nameof(SyncFileIssue.Message)).Contains("Textstring.config' in the publish output is different"));
    }

    [Test]
    public void Publish_Folder_With_Everything_Is_Fine()
    {
        var site = Temp("clean");
        Fixtures.CopyDirectory(Path.Combine(site, "uSync"), Path.Combine(site, "out", "uSync"));

        Assert.That(SyncPublishFolderCheck.Check(Path.Combine(site, "uSync", "v17"), site, Path.Combine(site, "out")), Is.Empty);
    }
}
