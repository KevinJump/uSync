using System;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

using Umbraco.Cms.Core.Services;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Services;
using uSync.Core.Serialization;

namespace uSync.Tests.Services;

[TestFixture]
public class SyncKeyMapTests
{
    private Mock<IKeyValueService> _keyValueService = null!;
    private uSyncSettings _settings = null!;
    private string? _stored;
    private int _writes;

    [SetUp]
    public void Setup()
    {
        _stored = null;
        _writes = 0;
        _settings = new uSyncSettings { KeyConflict = SyncKeyConflictMode.Map };

        _keyValueService = new Mock<IKeyValueService>();
        _keyValueService.Setup(x => x.GetValue(SyncKeyMap.StorageKey)).Returns(() => _stored);
        _keyValueService.Setup(x => x.SetValue(SyncKeyMap.StorageKey, It.IsAny<string>()))
            .Callback<string, string>((_, value) => { _stored = value; _writes++; });
    }

    private SyncKeyMap CreateMap()
    {
        var config = new Mock<ISyncConfigService>();
        config.Setup(x => x.Settings).Returns(() => _settings);
        return new SyncKeyMap(_keyValueService.Object, config.Object, NullLogger<SyncKeyMap>.Instance);
    }

    [Test]
    public void SetMode_IsDisabled_AndDoesNothing()
    {
        _settings.KeyConflict = SyncKeyConflictMode.Set;
        var map = CreateMap();
        var node = new XElement("Content", new XAttribute("Key", Guid.NewGuid()));

        map.Add("Content", Guid.NewGuid(), Guid.NewGuid());

        Assert.Multiple(() =>
        {
            Assert.That(map.Enabled, Is.False);
            Assert.That(map.Localize(node), Is.SameAs(node));
            Assert.That(_writes, Is.EqualTo(0));
        });
        _keyValueService.Verify(x => x.GetValue(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void Add_IsStored_AndReloaded()
    {
        var fileKey = Guid.NewGuid();
        var localKey = Guid.NewGuid();

        CreateMap().Add("Content", fileKey, localKey);

        var reloaded = CreateMap();
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.TryGetLocal(fileKey, out var found), Is.True);
            Assert.That(found, Is.EqualTo(localKey));
            Assert.That(reloaded.GetAll().Single().EntityType, Is.EqualTo("Content"));
        });
    }

    [Test]
    public void Add_SameKeys_IsIgnored()
    {
        var map = CreateMap();
        var key = Guid.NewGuid();

        map.Add("Content", key, key);
        map.Add("Content", Guid.Empty, Guid.NewGuid());

        Assert.That(map.GetAll(), Is.Empty);
        Assert.That(_writes, Is.EqualTo(0));
    }

    [Test]
    public void Localize_SwapsMappedKeys()
    {
        var map = CreateMap();
        var fileKey = Guid.NewGuid();
        var localKey = Guid.NewGuid();
        map.Add("Content", fileKey, localKey);

        var result = map.Localize(new XElement("Content", new XAttribute("Key", fileKey)));

        Assert.That(result.Attribute("Key")!.Value, Is.EqualTo(localKey.ToString()));
    }

    [Test]
    public void RemoveAndRemoveLocal()
    {
        var map = CreateMap();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var local = Guid.NewGuid();
        map.Add("Content", a, local);
        map.Add("Content", b, Guid.NewGuid());

        map.RemoveLocal(local);
        Assert.That(map.TryGetLocal(a, out _), Is.False);
        Assert.That(map.TryGetLocal(b, out _), Is.True);

        map.Remove(b);
        Assert.That(map.GetAll(), Is.Empty);
    }

    [Test]
    public void Clear_WorksWhenDisabled()
    {
        CreateMap().Add("Content", Guid.NewGuid(), Guid.NewGuid());
        _settings.KeyConflict = SyncKeyConflictMode.Set;

        var map = CreateMap();
        Assert.That(map.GetAll(), Has.Count.EqualTo(1));

        map.Clear();
        Assert.That(CreateMap().GetAll(), Is.Empty);
    }

    [Test]
    public void Batch_WritesOnceAtTheEnd()
    {
        var map = CreateMap();

        using (map.BeginBatch())
        {
            using (map.BeginBatch())
            {
                for (var i = 0; i < 10; i++)
                    map.Add("Content", Guid.NewGuid(), Guid.NewGuid());
            }

            Assert.That(_writes, Is.EqualTo(0));
        }

        Assert.That(_writes, Is.EqualTo(1));
        Assert.That(JsonSerializer.Deserialize<SyncKeyMapEntry[]>(_stored!), Has.Length.EqualTo(10));
    }

    [Test]
    public void BadStoredValue_StartsEmpty()
    {
        _stored = "not json";
        Assert.That(CreateMap().GetAll(), Is.Empty);
    }
}
