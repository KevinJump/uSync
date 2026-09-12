using NUnit.Framework;

using uSync.Core;

namespace uSync.Tests.Extensions;

[TestFixture]
public class PathNameTests
{
    [TestCase("c:\\somefile\\somepath\\myfile.txt")]
    [TestCase("c:\\website\\myfolder\\bob.config")]
    [TestCase("c:\\website\\myfolder\\0022C722-DB63-4388-B688-BB2F1BE342F9.config")]
    [TestCase("c:\\website\\myfolder\\Fred.config")]
    [TestCase("c:\\website\\myfolder\\apps.config")]
    public void GoodFileNamesAreNotChanged(string filename)
    {
        var name = filename.ToAppSafeFileName();

        Assert.That(name, Is.EqualTo(filename));    
    }

    [TestCase("c:\\website\\myfolder\\app.config", "c:\\website\\myfolder\\__app__.config")]
    [TestCase("c:\\website\\myfolder\\web.config", "c:\\website\\myfolder\\__web__.config")]
    public void BadFileNamesAreAppended(string filename, string expected)
    {
        var value = filename.ToAppSafeFileName();

        Assert.That(value, Is.EqualTo(expected));
    }

    [TestCase("c:\\website\\myfolder\\con.config")]
    [TestCase("c:\\website\\myfolder\\CON.config")]
    public void ReservedNamesAreNotChangedByDefault(string filename)
    {
        var value = filename.ToAppSafeFileName();

        Assert.That(value, Is.EqualTo(filename));
    }

    [TestCase("c:\\website\\myfolder\\readme.config", "c:\\website\\myfolder\\__readme__.config")]
    [TestCase("c:\\website\\myfolder\\README.CONFIG", "c:\\website\\myfolder\\__README__.CONFIG")]
    public void AdditionalBadNamesAreAppended(string filename, string expected)
    {
        var value = filename.ToAppSafeFileName(["readme.config"]);

        Assert.That(value, Is.EqualTo(expected));
    }

    [TestCase("c:\\website\\myfolder\\CON.config", "c:\\website\\myfolder\\__CON__.config")]
    [TestCase("c:\\website\\myfolder\\con.config", "c:\\website\\myfolder\\__con__.config")]
    [TestCase("c:\\website\\myfolder\\COM1.config", "c:\\website\\myfolder\\__COM1__.config")]
    [TestCase("c:\\website\\myfolder\\com9.config", "c:\\website\\myfolder\\__com9__.config")]
    [TestCase("c:\\website\\myfolder\\LPT1.config", "c:\\website\\myfolder\\__LPT1__.config")]
    [TestCase("c:\\website\\myfolder\\LPT9.config", "c:\\website\\myfolder\\__LPT9__.config")]
    public void WindowsReservedNamesAreAppendedWhenIncluded(string filename, string expected)
    {
        var value = filename.ToAppSafeFileName(StringExtensions.GetWindowsReservedNames());

        Assert.That(value, Is.EqualTo(expected));
    }

    [TestCase("c:\\website\\myfolder\\COM10.config")]
    [TestCase("c:\\website\\myfolder\\LPT0.config")]
    public void WindowsReservedNamesDoesNotOverreach(string filename)
    {
        var value = filename.ToAppSafeFileName(StringExtensions.GetWindowsReservedNames());

        Assert.That(value, Is.EqualTo(filename));
    }
}
