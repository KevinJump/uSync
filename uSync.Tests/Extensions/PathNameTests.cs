using NUnit.Framework;

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
    }
}
