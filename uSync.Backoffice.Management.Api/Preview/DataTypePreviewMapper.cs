using Umbraco.Cms.Api.Management.ViewModels.DataType;
using Umbraco.Cms.Core.Models;

using uSync.Backoffice.Management.Api.Models;
using uSync.BackOffice.Models;
using uSync.BackOffice.Preview;

using UdiEntityType = Umbraco.Cms.Core.Constants.UdiEntityType;

namespace uSync.Backoffice.Management.Api.Preview;

public class DataTypePreviewMapper : SyncPreviewMapperBase<IDataType>
{
    private readonly ISyncPreviewMappingHelper _helper;

    public DataTypePreviewMapper(ISyncPreviewMappingHelper helper)
    {
        _helper = helper;
    }

    public override string EntityType => UdiEntityType.DataType;

    protected override Task<object?> MapAsync(SyncItemPreview preview, IDataType? current, IDataType? incoming)
        => Task.FromResult<object?>(new SyncDataTypePreview
        {
            Current = _helper.Map<IDataType, DataTypeResponseModel>(current),
            Incoming = _helper.Map<IDataType, DataTypeResponseModel>(incoming),
        });
}
