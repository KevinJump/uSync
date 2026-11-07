using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;

using uSync.BackOffice.Models;
using uSync.BackOffice.Preview;

namespace uSync.Tests.Preview;

/// <summary>
///  the preview service maps a preview with the first mapper in the collection that can map it,
///  so packages can add mappers for their own items (or replace ours) without a switch on the type.
/// </summary>
[TestFixture]
public class SyncPreviewMapperCollectionTests
{
    [Test]
    public void Picks_Mapper_By_Item_Type()
    {
        var mappers = Build(b => b.Add<DataTypeMapper>().Add<ContentMapper>());

        Assert.That(mappers.GetMapper(Preview(current: Mock.Of<IContent>())), Is.TypeOf<ContentMapper>());
        Assert.That(mappers.GetMapper(Preview(current: Mock.Of<IDataType>())), Is.TypeOf<DataTypeMapper>());
    }

    [Test]
    public void Picks_Mapper_By_Incoming_Item_For_New_Items()
    {
        var mappers = Build(b => b.Add<DataTypeMapper>().Add<ContentMapper>());

        Assert.That(mappers.GetMapper(Preview(incoming: Mock.Of<IContent>())), Is.TypeOf<ContentMapper>());
    }

    [Test]
    public void No_Mapper_For_Items_Nobody_Maps()
    {
        var mappers = Build(b => b.Add<DataTypeMapper>());

        Assert.That(mappers.GetMapper(Preview(current: Mock.Of<IMedia>())), Is.Null);
    }

    [Test]
    public void Lower_Weight_Mapper_Replaces_Default_One()
    {
        // added after the default one, but the weight puts it first.
        var mappers = Build(b => b.Add<ContentMapper>().Add<PackageContentMapper>());

        var mapper = mappers.GetMapper(Preview(current: Mock.Of<IContent>()));
        Assert.That(mapper, Is.TypeOf<PackageContentMapper>());
        Assert.That(mapper.EntityType, Is.EqualTo("package-content"));
    }

    [Test]
    public async Task Base_Mapper_Passes_Typed_Items()
    {
        var current = Mock.Of<IContent>();
        var mapper = new ContentMapper();

        var model = await mapper.MapAsync(Preview(current: current));

        Assert.That(model, Is.EqualTo((current, (IContent)null)));
    }

    private static SyncPreviewMapperCollection Build(System.Action<SyncPreviewMapperCollectionBuilder> add)
    {
        var builder = new SyncPreviewMapperCollectionBuilder();
        add(builder);

        var services = new ServiceCollection();
        builder.RegisterWith(services);
        return services.BuildServiceProvider().GetRequiredService<SyncPreviewMapperCollection>();
    }

    private static SyncItemPreview Preview(object current = null, object incoming = null)
        => new() { Key = System.Guid.NewGuid(), Handler = "test", ItemType = "test", Current = current, Incoming = incoming };

    private class DataTypeMapper : SyncPreviewMapperBase<IDataType>
    {
        public override string EntityType => "data-type";

        protected override Task<object> MapAsync(SyncItemPreview preview, IDataType current, IDataType incoming)
            => Task.FromResult<object>(null);
    }

    private class ContentMapper : SyncPreviewMapperBase<IContent>
    {
        public override string EntityType => "document";

        protected override Task<object> MapAsync(SyncItemPreview preview, IContent current, IContent incoming)
            => Task.FromResult<object>((current, incoming));
    }

    [Weight(10)]
    private class PackageContentMapper : SyncPreviewMapperBase<IContent>
    {
        public override string EntityType => "package-content";

        protected override Task<object> MapAsync(SyncItemPreview preview, IContent current, IContent incoming)
            => Task.FromResult<object>(null);
    }
}
