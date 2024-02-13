using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.WebAssets;

namespace uSync.IntegrationTests.Datatypes;
partial class DataTypeTests
{
    [TestCase("Core - Color Picker")]
    public void DataTypeExits(string alias)
    {
        var dataType = _dataTypeService.GetDataType(alias);
        Assert.That(dataType, Is.Not.Null);
    }

    [TestCase("Core - Color Picker", 4)]
    public void ColorsArePicked(string alias, int count)
    {
        var dataType = _dataTypeService.GetDataType(alias);

        Assert.That(dataType, Is.Not.Null);

        var config = dataType.ConfigurationAs<ColorPickerConfiguration>();

        Assert.That(config, Is.Not.Null);
        Assert.That(config.UseLabel, Is.True);
        Assert.That(config.Items, Is.Not.Null);
        Assert.That(config.Items.Count, Is.EqualTo(count));
    }
}
