using uSync.Files;

namespace uSync.Files.Tests;

public class SyncFolderVerifierTests
{
    [Test]
    public void Clean_Folder_Has_No_Issues()
    {
        var result = Fixtures.Verify("clean");

        Assert.That(result.Issues, Is.Empty, string.Join(Environment.NewLine, result.Issues.Select(x => x.Message)));
        Assert.That(result.SkippedFolders, Is.EquivalentTo(new[] { "Content", "Media" }));
    }

    [Test]
    public void Clean_Folder_Has_No_Issues_With_Content()
    {
        var result = Fixtures.Verify("clean", content: true);

        Assert.That(result.Issues, Is.Empty, string.Join(Environment.NewLine, result.Issues.Select(x => x.Message)));
        Assert.That(result.SkippedFolders, Is.Empty);
        Assert.That(result.Items.Any(x => x.TypeFolder == "Content"), Is.True);
    }

    [Test]
    public void Conflict_In_BlockList_Content_Value_Is_Found_With_Content()
    {
        var result = Fixtures.Verify("conflict-blocklist-content", content: true);

        var conflicts = result.Issues(SyncFileRules.MergeConflict);
        Assert.That(conflicts, Has.Count.EqualTo(1));
        Assert.That(conflicts[0].Relative("conflict-blocklist-content"), Is.EqualTo("Content/home.config"));
        Assert.That(conflicts[0].Line, Is.EqualTo(17));
        Assert.That(conflicts[0].Message, Does.Contain("still valid XML"));

        // the CDATA keeps it valid XML, so there's no invalid xml error.
        Assert.That(result.Issues(SyncFileRules.InvalidXml), Is.Empty);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
    }

    [Test]
    public void Content_Conflict_Is_Not_Checked_Without_Content()
    {
        var result = Fixtures.Verify("conflict-blocklist-content");

        Assert.That(result.Issues, Is.Empty);
        Assert.That(result.SkippedFolders, Does.Contain("Content"));
    }

    [Test]
    public void Conflict_In_DataType_Config_CData_Is_Found()
    {
        var result = Fixtures.Verify("conflict-datatype-cdata");

        var conflicts = result.Issues(SyncFileRules.MergeConflict);
        Assert.That(conflicts, Has.Count.EqualTo(1));
        Assert.That(conflicts[0].Relative("conflict-datatype-cdata"), Is.EqualTo("DataTypes/BlockList.config"));
        Assert.That(result.Issues(SyncFileRules.InvalidXml), Is.Empty);
    }

    [Test]
    public void Conflict_That_Breaks_Xml_Reports_Conflict_Not_Invalid_Xml()
    {
        var result = Fixtures.Verify("conflict-broken-xml");

        var conflicts = result.Issues(SyncFileRules.MergeConflict);
        Assert.That(conflicts, Has.Count.EqualTo(1));
        Assert.That(conflicts[0].Line, Is.EqualTo(2));
        Assert.That(conflicts[0].Message, Does.Not.Contain("still valid XML"));
        Assert.That(result.Issues(SyncFileRules.InvalidXml), Is.Empty);
    }

    [Test]
    public void Invalid_Xml_Is_An_Error()
    {
        var result = Fixtures.Verify("invalid-xml");

        var invalid = result.Issues(SyncFileRules.InvalidXml);
        Assert.That(invalid, Has.Count.EqualTo(1));
        Assert.That(invalid[0].Relative("invalid-xml"), Is.EqualTo("Templates/broken.config"));
        Assert.That(invalid[0].Severity, Is.EqualTo(SyncFileSeverity.Error));
        Assert.That(result.Issues, Has.Count.EqualTo(1));
    }

    [Test]
    public void Duplicate_Keys_Are_Reported_On_Both_Files()
    {
        var result = Fixtures.Verify("duplicate-key");

        var duplicates = result.Issues(SyncFileRules.DuplicateKey);
        Assert.That(duplicates.Select(x => x.Relative("duplicate-key")),
            Is.EquivalentTo(new[] { "ContentTypes/article.config", "ContentTypes/article_copy.config" }));
        Assert.That(duplicates[0].Message, Does.Contain("c1000000-0000-0000-0000-000000000002"));
        Assert.That(result.Issues(SyncFileRules.AliasClash), Is.Empty);
    }

    [Test]
    public void Two_Branches_Adding_Article_Is_An_Alias_Clash()
    {
        var result = Fixtures.Verify("alias-clash-two-branches", content: true);

        var clashes = result.Issues(SyncFileRules.AliasClash);
        Assert.That(clashes.Select(x => x.Relative("alias-clash-two-branches")),
            Is.EquivalentTo(new[] { "ContentTypes/article.config", "ContentTypes/article_1.config" }));
        Assert.That(clashes[0].Message, Does.Contain("'article'"));

        // two content nodes called 'Home' is fine - content aliases are node names.
        Assert.That(clashes.Any(x => x.FilePath.Contains("Content" + Path.DirectorySeparatorChar)), Is.False);
        Assert.That(result.Issues.Count(x => x.Severity == SyncFileSeverity.Error), Is.EqualTo(2));
    }

    [Test]
    public void Delete_And_Live_Item_With_Same_Key_Is_An_Error()
    {
        var result = Fixtures.Verify("delete-and-live");

        var issues = result.Issues(SyncFileRules.DeleteAndLive);
        Assert.That(issues.Select(x => x.Relative("delete-and-live")),
            Is.EquivalentTo(new[] { "DataTypes/Textstring.config", "DataTypes/Textstring_deleted.config" }));

        // the empty file isn't a key clash, and the references to the data type still resolve.
        Assert.That(result.Issues(SyncFileRules.DuplicateKey), Is.Empty);
        Assert.That(result.Issues(SyncFileRules.BrokenReference), Is.Empty);
    }

    [Test]
    public void Broken_References_Are_Warnings()
    {
        var result = Fixtures.Verify("broken-references");

        var broken = result.Issues(SyncFileRules.BrokenReference);
        Assert.That(broken, Has.All.Property(nameof(SyncFileIssue.Severity)).EqualTo(SyncFileSeverity.Warning));

        var messages = broken.Select(x => x.Message).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(messages, Has.One.Contains("composition 'missingComposition'"));
            Assert.That(messages, Has.One.Contains("template 'missingTemplate'"));
            Assert.That(messages, Has.One.Contains("allowed child 'missingChild'"));
            Assert.That(messages, Has.One.Contains("data type (property 'intro')"));
            Assert.That(messages, Has.One.Contains("list view data type"));
            Assert.That(messages, Has.One.Contains("parent template 'missingMaster'"));
        });
        Assert.That(broken, Has.Count.EqualTo(6));

        // a reference has the line of the element that makes it.
        var composition = broken.Single(x => x.Message.Contains("missingComposition"));
        Assert.That(composition.Line, Is.GreaterThan(2));

        Assert.That(result.ErrorCount, Is.Zero);
    }

    [Test]
    public void Older_Usync_Config_Than_Package_Is_A_Warning()
    {
        var result = Fixtures.Verify("old-format");

        var old = result.Issues(SyncFileRules.OldFormat);
        Assert.That(old, Has.Count.EqualTo(1));
        Assert.That(old[0].Relative("old-format"), Is.EqualTo("usync.config"));
        Assert.That(old[0].Message, Does.Contain("16.0.2").And.Contain("17.2.0"));
    }

    [Test]
    public void Same_Major_Version_Is_Not_Old()
    {
        // clean has uSync 17.0.0 in its Site.csproj and a 17.0.0.0 usync.config
        var result = Fixtures.Verify("clean");
        Assert.That(result.Issues(SyncFileRules.OldFormat), Is.Empty);
    }

    [Test]
    public void Unknown_Folders_And_Files_Are_Warnings()
    {
        var result = Fixtures.Verify("unknown-files");

        var unknown = result.Issues(SyncFileRules.UnknownFile);
        Assert.That(unknown.Select(x => x.Relative("unknown-files")),
            Is.EquivalentTo(new[] { "ContentTypes/readme.txt", "Junk", "stray.txt" }));

        // a custom handler's folder, whose files are uSync items, is fine.
        Assert.That(unknown.Any(x => x.FilePath.Contains("MyCustomObjects")), Is.False);
        Assert.That(result.ErrorCount, Is.Zero);
    }

    [Test]
    public void Content_With_Missing_Content_Type_Is_A_Warning_With_Content()
    {
        var result = Fixtures.Verify("missing-content-type", content: true);

        var missing = result.Issues(SyncFileRules.MissingContentType);
        Assert.That(missing.Select(x => x.Relative("missing-content-type")),
            Is.EquivalentTo(new[] { "Content/landing.config", "Media/clip.config" }));
        Assert.That(missing.Single(x => x.FilePath.EndsWith("landing.config")).Message, Does.Contain("'landingPage'"));
        Assert.That(missing.Single(x => x.FilePath.EndsWith("clip.config")).Message, Does.Contain("media type 'Video'"));
    }

    [Test]
    public void Missing_Content_Type_Is_Not_Checked_Without_Content()
    {
        var result = Fixtures.Verify("missing-content-type");
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void Missing_Folder_Throws()
    {
        Assert.Throws<SyncFolderNotFoundException>(() =>
            new SyncFolderVerifier().Verify(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }
}
