using System;

using NUnit.Framework;

using Umbraco.Cms.Core.Models.Entities;

using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.Tests.Serializers;

[TestFixture]
public class SyncKeyHelperTests
{
    private sealed class TestEntity : EntityBase { }

    [Test]
    public void SetKey_UnsavedEntity_SetsKey()
    {
        var entity = new TestEntity();
        var key = Guid.NewGuid();

        SyncKeyHelper.SetKey(entity, key, "test");

        Assert.That(entity.Key, Is.EqualTo(key));
    }

    [Test]
    public void SetKey_SameKey_DoesNothing()
    {
        var key = Guid.NewGuid();
        var entity = new TestEntity { Id = 10, Key = key };

        Assert.DoesNotThrow(() => SyncKeyHelper.SetKey(entity, key, "test"));
        Assert.That(entity.Key, Is.EqualTo(key));
    }

    [Test]
    public void SetKey_SavedEntity_SetsKeyOrThrowsKeyConflict()
    {
        var localKey = Guid.NewGuid();
        var fileKey = Guid.NewGuid();
        var entity = new TestEntity { Id = 10, Key = localKey };

        if (SyncKeyHelper.KeysAreImmutable)
        {
            // Umbraco 18+
            var ex = Assert.Throws<uSyncKeyConflictException>(
                () => SyncKeyHelper.SetKey(entity, fileKey, "home"));

            Assert.That(ex!.LocalKey, Is.EqualTo(localKey));
            Assert.That(ex.FileKey, Is.EqualTo(fileKey));
            Assert.That(ex.Alias, Is.EqualTo("home"));
            Assert.That(ex.ItemType, Is.EqualTo(nameof(TestEntity)));
            Assert.That(ex.InnerException, Is.InstanceOf<InvalidOperationException>());
            Assert.That(entity.Key, Is.EqualTo(localKey));
        }
        else
        {
            // before Umbraco 18
            SyncKeyHelper.SetKey(entity, fileKey, "home");
            Assert.That(entity.Key, Is.EqualTo(fileKey));
        }
    }

    [Test]
    public void IsKeyConflict_MatchesKeysAreImmutable()
    {
        Assert.That(SyncKeyHelper.IsKeyConflict(Guid.NewGuid(), Guid.NewGuid()),
            Is.EqualTo(SyncKeyHelper.KeysAreImmutable));
    }

    [Test]
    public void IsKeyConflict_SameOrEmptyKeys_IsFalse()
    {
        var key = Guid.NewGuid();

        Assert.That(SyncKeyHelper.IsKeyConflict(key, key), Is.False);
        Assert.That(SyncKeyHelper.IsKeyConflict(Guid.Empty, key), Is.False);
        Assert.That(SyncKeyHelper.IsKeyConflict(key, Guid.Empty), Is.False);
    }

    [Test]
    public void KeyConflictMessage_HasDetailsAndDocsLink()
    {
        var localKey = Guid.NewGuid();
        var fileKey = Guid.NewGuid();

        var message = new uSyncKeyConflictException("Content", "Home", localKey, fileKey).Message;

        Assert.That(message, Does.StartWith("Key conflict:"));
        Assert.That(message, Does.Contain("'Home' (Content)"));
        Assert.That(message, Does.Contain(localKey.ToString()));
        Assert.That(message, Does.Contain(fileKey.ToString()));
        Assert.That(message, Does.Contain(uSyncConstants.KeyConflictDocsUrl));
    }
}
