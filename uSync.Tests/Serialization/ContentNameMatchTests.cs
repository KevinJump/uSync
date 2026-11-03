using NUnit.Framework;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Strings;

using uSync.Core;

namespace uSync.Tests.Serialization
{
    /// <summary>
    ///  When a content/media key doesn't match, uSync falls back to finding a
    ///  sibling with the same name. The uSync file holds the name as it is,
    ///  so the match has to work for names with spaces, hyphens, etc.
    /// </summary>
    /// <remarks>
    ///  Repro for the duplicate "(1)" nodes in uSync.Complete.Issues #314.
    /// </remarks>
    [TestFixture]
    public class ContentNameMatchTests
    {
        private IShortStringHelper _shortStringHelper;

        [SetUp]
        public void Setup()
        {
            _shortStringHelper = new DefaultShortStringHelper(
                new DefaultShortStringHelperConfig().WithDefault(new RequestHandlerSettings()));
        }

        [TestCase("home")]
        [TestCase("Home")]
        [TestCase("M126680-0001")]
        [TestCase("Home Page")]
        [TestCase("News & Events")]
        public void SameName_IsFoundByNameFallback(string name)
        {
            Assert.That(name.IsNameMatch(name, _shortStringHelper), Is.True);
        }

        [TestCase("Home Page", "home page")]
        [TestCase("Home Page", "HomePage")]
        public void SafeAliasEquivalentNames_Match(string name, string alias)
        {
            Assert.That(name.IsNameMatch(alias, _shortStringHelper), Is.True);
        }

        [TestCase("M126680-0001", "M126680-0002")]
        [TestCase("Home", "")]
        [TestCase("", "")]
        [TestCase("1", "4")]
        [TestCase("2024", "2025")]
        [TestCase("一", "二")]
        public void DifferentNames_DoNotMatch(string name, string alias)
        {
            Assert.That(name.IsNameMatch(alias, _shortStringHelper), Is.False);
        }
    }
}
