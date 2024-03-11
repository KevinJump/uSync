using NUnit.Framework;

using Umbraco.Extensions;

using uSync.BackOffice.Extensions;
using uSync.Core;

namespace uSync.Tests.Extensions
{
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

            Assert.AreEqual(filename, name);
        }

        [TestCase("c:\\website\\myfolder\\app.config", "c:\\website\\myfolder\\__app__.config")]
        [TestCase("c:\\website\\myfolder\\web.config", "c:\\website\\myfolder\\__web__.config")]
        public void BadFileNamesAreAppended(string filename, string expected)
        {
            var value = filename.ToAppSafeFileName();

            Assert.AreEqual(expected, value);
        }

        [TestCase("C:\\Source\\OpenSource\\v13\\uSync\\README.md", "\\OpenSource\\v13\\uSync")]
        [TestCase("C:\\Source\\OpenSource\\v13\\README.md", "\\Source\\OpenSource\\v13")]
        [TestCase("C:\\Source\\OpenSource\\README.md", "\\Source\\OpenSource")]
        [TestCase("C:\\Source\\README.md", "\\Source")]
        [TestCase("C:\\Source/OpenSource\\v13\\uSync\\README.md", "\\OpenSource\\v13\\uSync")]
        [TestCase("C:\\Source/OpenSource\\v13\\README.md", "\\Source\\OpenSource\\v13")]
        [TestCase("C:\\Source/OpenSource/README.md", "\\Source\\OpenSource")]
        [TestCase("C:/Source\\README.md", "\\Source")]
        [TestCase("README.md", "")]
        [TestCase("", "")]
        public void TruncatedPathsWithoutFileName(string filename, string expected)
        {
            var result = filename.TruncatePath(3, false);

            Assert.AreEqual(expected, result);
        }

        [TestCase("C:\\Source\\OpenSource\\v13\\uSync\\README.md", "\\v13\\uSync\\README.md")]
        [TestCase("C:\\Source\\OpenSource\\v13\\README.md", "\\OpenSource\\v13\\README.md")]
        [TestCase("C:\\Source\\OpenSource\\README.md", "\\Source\\OpenSource\\README.md")]
        [TestCase("C:\\Source\\README.md", "\\Source\\README.md")]
        [TestCase("README.md", "\\README.md")]
        [TestCase("", "")]
        public void TruncatedPathsWithFileName(string filename, string expected)
        {
            var result = filename.TruncatePath(3, true);

            Assert.AreEqual(expected, result);
        }
    }
}
