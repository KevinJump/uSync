using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

using uSync.BackOffice;
using uSync.Core;
using uSync.Core.Models;

namespace uSync.Tests.Extensions;

/// <summary>
///  tests for the action index used to look up actions during the second pass import.
/// </summary>
[TestFixture]
public class ActionIndexTests
{
    private const string _handler = "ContentHandler";

    private static readonly Guid _one = Guid.Parse("{5E37F691-FF91-45DA-9F53-8641FD9FE233}");
    private static readonly Guid _two = Guid.Parse("{5A35701C-349C-4AAE-BBCE-964B5C196989}");
    private static readonly Guid _three = Guid.Parse("{C70BE6CF-4923-4E2B-8742-B90FB7BBAFCB}");

    private static List<uSyncAction> GetActions()
        => new List<uSyncAction>
        {
            new uSyncAction { Key = _one, HandlerAlias = _handler, Name = "one", Success = true },
            new uSyncAction { Key = _two, HandlerAlias = _handler, Name = "two", Success = true },
            new uSyncAction { Key = _three, HandlerAlias = "OtherHandler", Name = "three", Success = true },
        };

    [Test]
    public void IndexFindsActionByKeyAndAlias()
    {
        var actions = GetActions();
        var index = actions.CreateActionIndex();

        Assert.That(index[(_one, _handler)], Is.EqualTo(0));
        Assert.That(index[(_two, _handler)], Is.EqualTo(1));
        Assert.That(index.ContainsKey((_two, "OtherHandler")), Is.False);
    }

    [Test]
    public void DuplicateKeysIndexTheFirstAction()
    {
        var actions = GetActions();
        actions.Add(new uSyncAction { Key = _one, HandlerAlias = _handler, Name = "one-again" });

        var index = actions.CreateActionIndex();

        Assert.That(index[(_one, _handler)], Is.EqualTo(0));
    }

    [Test]
    public void UpdateWithIndexUpdatesActionInPlace()
    {
        var actions = GetActions();
        var index = actions.CreateActionIndex();

        actions.UpdateActions(index, _one, _handler, SyncAttempt<string>.Succeed("one", ChangeType.Import, " updated"));

        Assert.That(actions.Count, Is.EqualTo(3));
        Assert.That(actions[0].Name, Is.EqualTo("one"));
        Assert.That(actions[0].Message, Is.EqualTo(" updated"));
        Assert.That(actions[0].Success, Is.True);
    }

    [Test]
    public void UpdateWithIndexMarksFailures()
    {
        var actions = GetActions();
        var index = actions.CreateActionIndex();

        actions.UpdateActions(index, _two, _handler, SyncAttempt<string>.Fail("two", ChangeType.ImportFail, "it broke"));

        Assert.That(actions[1].Success, Is.False);
        Assert.That(actions[1].Change, Is.EqualTo(ChangeType.Fail));
        Assert.That(actions[1].Message, Is.EqualTo("Failed: it broke"));
    }

    [Test]
    public void UpdateWithIndexIgnoresMissingActions()
    {
        var actions = GetActions();
        var index = actions.CreateActionIndex();

        // right key, wrong handler - nothing should change.
        actions.UpdateActions(index, _one, "MissingHandler", SyncAttempt<string>.Succeed("one", ChangeType.Import, "updated"));

        Assert.That(actions.Count, Is.EqualTo(3));
        Assert.That(actions.All(x => string.IsNullOrEmpty(x.Message)), Is.True);
    }

    [Test]
    public void UpdateWithIndexSkipsUneventfulAttempts()
    {
        var actions = GetActions();
        var index = actions.CreateActionIndex();

        // success, no message, no details - nothing worth updating.
        actions.UpdateActions(index, _one, _handler,
            SyncAttempt<string>.Succeed("one", "item", ChangeType.NoChange, new List<uSyncChange>()));

        Assert.That(actions[0].Details, Is.Null);
        Assert.That(actions[0].Message, Is.Null);
    }

    [Test]
    public void TryFindActionMatchesKeyAndAlias()
    {
        var actions = GetActions();

        Assert.That(actions.TryFindAction(_two, _handler, out var action), Is.True);
        Assert.That(action.Name, Is.EqualTo("two"));

        Assert.That(actions.TryFindAction(_two, "OtherHandler", out _), Is.False);
    }
}
