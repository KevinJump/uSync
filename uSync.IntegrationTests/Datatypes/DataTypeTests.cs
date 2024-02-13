using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Services;

namespace uSync.IntegrationTests.Datatypes;
public partial class DataTypeTests : IntegrationTestBase
{
    private readonly Setup _setup = new();

    private IDataTypeService _dataTypeService;

    [SetUp]
    public void Setup()
    {
        _dataTypeService = _setup.ServiceProvider.GetRequiredService<IDataTypeService>();
    }
    [TearDown]
    public void TearDown()
    {
        _setup.Dispose();
    }

       
}
