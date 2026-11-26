using uSync.Files;

namespace uSync.Files.Tests;

public class ConflictMarkerScannerTests
{
    [Test]
    public void Finds_A_Conflict_Block()
    {
        var text = "a\n<<<<<<< HEAD\nb\n=======\nc\n>>>>>>> branch\nd";
        var blocks = ConflictMarkerScanner.Scan(text);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0].StartLine, Is.EqualTo(2));
        Assert.That(blocks[0].EndLine, Is.EqualTo(6));
    }

    [Test]
    public void Finds_Diff3_Blocks_With_Windows_Line_Endings()
    {
        var text = "<<<<<<< ours\r\nb\r\n||||||| base\r\nx\r\n=======\r\nc\r\n>>>>>>> theirs\r\n";
        var blocks = ConflictMarkerScanner.Scan(text);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0].EndLine, Is.EqualTo(7));
    }

    [Test]
    public void Finds_More_Than_One_Block()
    {
        var text = "<<<<<<< a\n=======\n>>>>>>> b\nok\n<<<<<<< a\n=======\n>>>>>>> b";
        Assert.That(ConflictMarkerScanner.Scan(text), Has.Count.EqualTo(2));
    }

    [Test]
    public void Finds_A_Stray_Marker()
    {
        Assert.That(ConflictMarkerScanner.Scan("one\n>>>>>>> branch\n"), Has.Count.EqualTo(1));
        Assert.That(ConflictMarkerScanner.Scan("one\n=======\n"), Has.Count.EqualTo(1));
    }

    [TestCase("a ======= b")]
    [TestCase("  =======")]
    [TestCase("========")]
    [TestCase("<<<<<< six")]
    [TestCase("<p>text</p>")]
    public void Ignores_Things_That_Are_Not_Markers(string line)
    {
        Assert.That(ConflictMarkerScanner.Scan($"x\n{line}\ny"), Is.Empty);
    }
}
