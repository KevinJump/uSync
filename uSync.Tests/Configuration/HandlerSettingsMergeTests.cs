using NUnit.Framework;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Extensions;

namespace uSync.Tests.Configuration;

/// <summary>
///  Tests for how a handler's own settings are merged with the HandlerDefaults
///  when resolved via <see cref="HandlerSetSettingsExtensions.GetHandlerSettings"/>.
/// </summary>
[TestFixture]
public class HandlerSettingsMergeTests
{
    private static uSyncHandlerSetSettings BuildSet()
    {
        var set = new uSyncHandlerSetSettings
        {
            HandlerDefaults = new HandlerSettings
            {
                UseFlatStructure = false,
            }
        };

        set.HandlerDefaults.Settings["CreateOnly"] = "false";
        set.HandlerDefaults.Settings["FromDefaults"] = "default-value";

        return set;
    }

    [Test]
    public void HandlerWithoutOwnBlock_UsesDefaults()
    {
        var set = BuildSet();

        var settings = set.GetHandlerSettings("ContentHandler");

        Assert.Multiple(() =>
        {
            Assert.That(settings.IsCreateOnly(), Is.False);
            Assert.That(settings.GetSetting("FromDefaults", string.Empty), Is.EqualTo("default-value"));
            Assert.That(settings.UseFlatStructure, Is.False);
        });
    }

    [Test]
    public void HandlerOwnKey_OverridesDefaultKey()
    {
        // the #953 scenario: HandlerDefaults set CreateOnly false,
        // the named handler explicitly turns it on.
        var set = BuildSet();
        var handler = new HandlerSettings();
        handler.Settings["CreateOnly"] = "true";
        set.Handlers["ContentHandler"] = handler;

        var settings = set.GetHandlerSettings("ContentHandler");

        Assert.That(settings.IsCreateOnly(), Is.True);
    }

    [Test]
    public void HandlerWithOwnBlock_InheritsDefaultKeysItDoesNotSet()
    {
        // previously a handler with its own block ignored HandlerDefaults entirely.
        var set = BuildSet();
        var handler = new HandlerSettings();
        handler.Settings["CreateOnly"] = "true";
        set.Handlers["ContentHandler"] = handler;

        var settings = set.GetHandlerSettings("ContentHandler");

        Assert.That(settings.GetSetting("FromDefaults", string.Empty), Is.EqualTo("default-value"));
    }

    [Test]
    public void Merge_DoesNotMutateInputs()
    {
        var set = BuildSet();
        var handler = new HandlerSettings();
        handler.Settings["CreateOnly"] = "true";
        set.Handlers["ContentHandler"] = handler;

        _ = set.GetHandlerSettings("ContentHandler");

        Assert.Multiple(() =>
        {
            // defaults should not have gained the handler's key
            Assert.That(set.HandlerDefaults.Settings.ContainsKey("CreateOnly"), Is.True);
            Assert.That(set.HandlerDefaults.Settings["CreateOnly"], Is.EqualTo("false"));
            // handler block should not have gained the default's key
            Assert.That(handler.Settings.ContainsKey("FromDefaults"), Is.False);
        });
    }

    [Test]
    public void Clone_PreservesCreateCleanAndFullFileOnDifference()
    {
        // regression: Clone() used to drop these two properties.
        var settings = new HandlerSettings
        {
            CreateClean = true,
            FullFileOnDifference = true,
        };

        var clone = settings.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(clone.CreateClean, Is.True);
            Assert.That(clone.FullFileOnDifference, Is.True);
        });
    }

    [Test]
    public void MergeWithDefaults_TakesStronglyTypedPropertiesFromHandler()
    {
        var defaults = new HandlerSettings { GuidNames = true, CreateClean = true };
        var handler = new HandlerSettings { GuidNames = false, CreateClean = false };

        var merged = handler.MergeWithDefaults(defaults);

        Assert.Multiple(() =>
        {
            Assert.That(merged.GuidNames, Is.False);
            Assert.That(merged.CreateClean, Is.False);
        });
    }
}
