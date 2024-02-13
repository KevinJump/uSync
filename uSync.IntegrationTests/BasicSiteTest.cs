using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Services;

using uSync.BackOffice.Services;

namespace uSync.IntegrationTests;
public class BasicSiteTest : IntegrationTestBase
{
    private readonly Setup _setup = new();

    [TearDown]
    public void TearDown()
    {
        _setup.Dispose();
    }

    [Test]
    public void CheckSiteBuilds()
    {
        var result = _setup.ServiceProvider.GetRequiredService<ISyncActionService>();
        Assert.That(result, Is.Not.Null);
    }


    [TestCase(0)]
    public void HasContent(int count)
    {
        var contentService = _setup.ServiceProvider.GetRequiredService<IContentService>();
        var rootNodes = contentService.GetRootContent().Count();

        Assert.That(rootNodes, Is.EqualTo(0));
    }

    [TestCase(39)]
    public void HasDataTypes(int count)
    {
        var dataTypeService = _setup.ServiceProvider.GetRequiredService<IDataTypeService>();
        var dataTypes = dataTypeService.GetAll();

        Assert.That(dataTypes.Count(), Is.EqualTo(count));
    }

    [TestCase(1)]
    public void HasLanguages(int count)
    {
        var languageService = _setup.ServiceProvider.GetRequiredService<ILocalizationService>();

        var languages = languageService.GetAllLanguages();

        Assert.That(languages.Count(), Is.EqualTo(count));
    }

}
