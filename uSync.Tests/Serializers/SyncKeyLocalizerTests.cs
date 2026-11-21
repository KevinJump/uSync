using System;
using System.Collections.Generic;
using System.Xml.Linq;

using NUnit.Framework;

using Umbraco.Cms.Core.Models.Entities;

using uSync.Core.Serialization;

namespace uSync.Tests.Serializers;

[TestFixture]
public class SyncKeyLocalizerTests
{
    private static readonly Guid _fileKey = Guid.Parse("7f3a1c2e-4b5d-4e6f-8a9b-0c1d2e3f4a5b");
    private static readonly Guid _localKey = Guid.Parse("11111111-2222-4333-8444-555555555555");

    private static readonly Dictionary<Guid, Guid> _map = new() { [_fileKey] = _localKey };

    [Test]
    public void EmptyMap_ReturnsSameNode()
    {
        var node = new XElement("Content", new XAttribute("Key", _fileKey));
        Assert.That(SyncKeyLocalizer.Localize(node, new Dictionary<Guid, Guid>()), Is.SameAs(node));
    }

    [Test]
    public void NothingMapped_ReturnsSameNode()
    {
        var node = new XElement("Content", new XAttribute("Key", Guid.NewGuid()));
        Assert.That(SyncKeyLocalizer.Localize(node, _map), Is.SameAs(node));
    }

    [Test]
    public void SwapsDashedKeyInAttributesAndText()
    {
        var node = new XElement("Content",
            new XAttribute("Key", _fileKey),
            new XElement("Info", new XElement("Parent", new XAttribute("Key", _fileKey), "home")),
            new XElement("Value", new XCData($"[\"{_fileKey}\"]")));

        var result = SyncKeyLocalizer.Localize(node, _map);

        Assert.That(result, Is.Not.SameAs(node));
        Assert.That(result.ToString(), Does.Not.Contain(_fileKey.ToString()));
        Assert.That(result.Attribute("Key")!.Value, Is.EqualTo(_localKey.ToString()));
        Assert.That(result.Element("Info")!.Element("Parent")!.Attribute("Key")!.Value, Is.EqualTo(_localKey.ToString()));
        Assert.That(result.Element("Value")!.Value, Is.EqualTo($"[\"{_localKey}\"]"));

        // the original isn't changed.
        Assert.That(node.Attribute("Key")!.Value, Is.EqualTo(_fileKey.ToString()));
    }

    [Test]
    public void SwapsUdiFormat()
    {
        var node = new XElement("Value", $"umb://document/{_fileKey:N}");

        var result = SyncKeyLocalizer.Localize(node, _map);

        Assert.That(result.Value, Is.EqualTo($"umb://document/{_localKey:N}"));
    }

    [Test]
    public void KeepsUpperCase()
    {
        var node = new XElement("Value", _fileKey.ToString().ToUpperInvariant());

        var result = SyncKeyLocalizer.Localize(node, _map);

        Assert.That(result.Value, Is.EqualTo(_localKey.ToString().ToUpperInvariant()));
    }

    [Test]
    public void IgnoresKeyInsideLongerHex()
    {
        var hash = $"{_fileKey:N}{_fileKey:N}";
        var node = new XElement("Value", hash);

        Assert.That(SyncKeyLocalizer.Localize(node, _map), Is.SameAs(node));
    }

    [Test]
    public void KeepsWhitespaceInValues()
    {
        var node = XElement.Parse($"<Value>  line one\n  {_fileKey}\n</Value>", LoadOptions.PreserveWhitespace);

        var result = SyncKeyLocalizer.Localize(node, _map);

        Assert.That(result.Value, Is.EqualTo($"  line one\n  {_localKey}\n"));
    }
}

[TestFixture]
public class SyncKeyHelperMapScopeTests
{
    private sealed class TestEntity : EntityBase { }

    private sealed class RecordingMap : ISyncKeyMap
    {
        public bool Enabled { get; set; } = true;
        public List<(string type, Guid fileKey, Guid localKey)> Added { get; } = [];

        public bool TryGetLocal(Guid fileKey, out Guid localKey) { localKey = Guid.Empty; return false; }
        public void Add(string entityType, Guid fileKey, Guid localKey) => Added.Add((entityType, fileKey, localKey));
        public void Remove(Guid fileKey) { }
        public void RemoveLocal(Guid localKey) { }
        public void Clear() { }
        public IReadOnlyCollection<SyncKeyMapEntry> GetAll() => [];
        public XElement Localize(XElement node) => node;
        public IDisposable BeginBatch() => NullSyncKeyMap.Instance.BeginBatch();
    }

    [Test]
    public void InScope_SavedEntity_IsMappedNotChanged()
    {
        var map = new RecordingMap();
        var localKey = Guid.NewGuid();
        var fileKey = Guid.NewGuid();
        var entity = new TestEntity { Id = 5, Key = localKey };

        using (SyncKeyHelper.BeginKeyMapScope(map))
        {
            SyncKeyHelper.SetKey(entity, fileKey, "home");
        }

        Assert.That(entity.Key, Is.EqualTo(localKey));
        Assert.That(map.Added, Is.EquivalentTo(new[] { (nameof(TestEntity), fileKey, localKey) }));
    }

    [Test]
    public void InScope_NewEntity_GetsFileKey()
    {
        var map = new RecordingMap();
        var fileKey = Guid.NewGuid();
        var entity = new TestEntity();

        using (SyncKeyHelper.BeginKeyMapScope(map))
        {
            SyncKeyHelper.SetKey(entity, fileKey, "home");
        }

        Assert.That(entity.Key, Is.EqualTo(fileKey));
        Assert.That(map.Added, Is.Empty);
    }

    [Test]
    public void DisabledMap_DoesNotOpenScope()
    {
        var map = new RecordingMap { Enabled = false };
        var entity = new TestEntity { Id = 5, Key = Guid.NewGuid() };

        using (SyncKeyHelper.BeginKeyMapScope(map))
        {
            if (SyncKeyHelper.KeysAreImmutable)
                Assert.Throws<uSyncKeyConflictException>(() => SyncKeyHelper.SetKey(entity, Guid.NewGuid(), "home"));
            else
                SyncKeyHelper.SetKey(entity, Guid.NewGuid(), "home");
        }

        Assert.That(map.Added, Is.Empty);
    }

    [Test]
    public void ScopeEnds_OnDispose()
    {
        var map = new RecordingMap();
        var localKey = Guid.NewGuid();
        var fileKey = Guid.NewGuid();
        var entity = new TestEntity { Id = 5, Key = localKey };

        using (SyncKeyHelper.BeginKeyMapScope(map)) { }

        if (SyncKeyHelper.KeysAreImmutable)
            Assert.Throws<uSyncKeyConflictException>(() => SyncKeyHelper.SetKey(entity, fileKey, "home"));
        else
            SyncKeyHelper.SetKey(entity, fileKey, "home");

        Assert.That(map.Added, Is.Empty);
    }
}
