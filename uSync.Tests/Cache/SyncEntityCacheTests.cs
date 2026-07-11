using System;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;

using uSync.Core.Cache;

namespace uSync.Tests.Cache;

[TestFixture]
internal class SyncEntityCacheTests
{
    private Mock<IEntityService> _entityServiceMock;
    private Mock<IContentTypeService> _contentTypeServiceMock;
    private SyncEntityCache _cache;

    [SetUp]
    public void Setup()
    {
        _entityServiceMock = new Mock<IEntityService>();
        _contentTypeServiceMock = new Mock<IContentTypeService>();
        _cache = new SyncEntityCache(_entityServiceMock.Object, _contentTypeServiceMock.Object);
    }

    [Test]
    public void AddName_ThenGetName_RoundTrips()
    {
        var id = 1234;
        var key = Guid.NewGuid();

        _cache.AddName(id, key, "Test Name");

        var result = _cache.GetName(id);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.Name, Is.EqualTo("Test Name"));
        });
    }

    // regression for uSync.Complete issue #304 - GetName used to read from the
    // entity cache, which holds IEntitySlim objects under the same id key. That
    // threw a swallowed InvalidCastException (IEntitySlim -> CachedName) and
    // never returned the name. GetName must read from the name cache instead.
    [Test]
    public void GetName_WhenEntityCachedUnderSameId_StillReturnsName()
    {
        var id = 4321;
        var key = Guid.NewGuid();

        var entityMock = new Mock<IEntitySlim>();
        entityMock.SetupGet(x => x.Id).Returns(id);
        _entityServiceMock.Setup(x => x.Get(id)).Returns(entityMock.Object);

        // populate the entity cache for this id (as GetFriendlyPath does).
        _ = _cache.GetEntity(id);

        _cache.AddName(id, key, "Real Name");

        var result = _cache.GetName(id);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Key, Is.EqualTo(key));
            Assert.That(result.Name, Is.EqualTo("Real Name"));
        });
    }
}
