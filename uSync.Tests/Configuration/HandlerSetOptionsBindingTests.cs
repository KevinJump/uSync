using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NUnit.Framework;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Extensions;

namespace uSync.Tests.Configuration;

/// <summary>
///  Tests that <see cref="HandlerSetConfigurationExtensions.ConfigureHandlerSet"/> layers a handler's
///  own configuration over the top of the set's HandlerDefaults - including the strongly typed
///  properties that can only be merged at bind time.
/// </summary>
[TestFixture]
public class HandlerSetOptionsBindingTests
{
    private const string SetPath = "uSync:Sets:Default";

    private static uSyncHandlerSetSettings ResolveSet(Dictionary<string, string> config)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.ConfigureHandlerSet("Default", configuration.GetSection(SetPath));

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<uSyncHandlerSetSettings>>().Get("Default");
    }

    [Test]
    public void StronglyTypedPropertiesCascadeFromDefaults()
    {
        // ContentHandler only sets CreateOnly - everything else should come from HandlerDefaults.
        var set = ResolveSet(new()
        {
            [$"{SetPath}:HandlerDefaults:UseFlatStructure"] = "false",
            [$"{SetPath}:HandlerDefaults:GuidNames"] = "true",
            [$"{SetPath}:HandlerDefaults:Settings:CreateOnly"] = "false",
            [$"{SetPath}:HandlerDefaults:Settings:FromDefaults"] = "default-value",
            [$"{SetPath}:Handlers:ContentHandler:Settings:CreateOnly"] = "true",
        });

        var settings = set.GetHandlerSettings("ContentHandler");

        Assert.Multiple(() =>
        {
            // strongly typed defaults now cascade (the bit that needed the options layer)
            Assert.That(settings.UseFlatStructure, Is.False, "UseFlatStructure should inherit from defaults");
            Assert.That(settings.GuidNames, Is.True, "GuidNames should inherit from defaults");
            // dictionary merge
            Assert.That(settings.IsCreateOnly(), Is.True, "handler's own CreateOnly should win");
            Assert.That(settings.GetSetting("FromDefaults", string.Empty), Is.EqualTo("default-value"));
        });
    }

    [Test]
    public void HandlerCanOverrideStronglyTypedDefault()
    {
        var set = ResolveSet(new()
        {
            [$"{SetPath}:HandlerDefaults:UseFlatStructure"] = "false",
            [$"{SetPath}:HandlerDefaults:GuidNames"] = "true",
            [$"{SetPath}:Handlers:FlatHandler:UseFlatStructure"] = "true",
        });

        var settings = set.GetHandlerSettings("FlatHandler");

        Assert.Multiple(() =>
        {
            Assert.That(settings.UseFlatStructure, Is.True, "handler override should win over default");
            Assert.That(settings.GuidNames, Is.True, "unset property should still inherit from defaults");
        });
    }

    [Test]
    public void MinimalBlockInheritsAllUnsetDefaults()
    {
        // a handler that only sets one property (here Enabled) must still inherit every
        // other default - this is the "lots of handlers with near-empty blocks" case.
        var set = ResolveSet(new()
        {
            [$"{SetPath}:HandlerDefaults:UseFlatStructure"] = "false",
            [$"{SetPath}:HandlerDefaults:GuidNames"] = "true",
            [$"{SetPath}:HandlerDefaults:Settings:CreateOnly"] = "true",
            [$"{SetPath}:Handlers:MinimalHandler:Enabled"] = "true",
        });

        var settings = set.GetHandlerSettings("MinimalHandler");

        Assert.Multiple(() =>
        {
            Assert.That(settings.Enabled, Is.True);
            Assert.That(settings.UseFlatStructure, Is.False, "should inherit default, not the C# default of true");
            Assert.That(settings.GuidNames, Is.True);
            Assert.That(settings.IsCreateOnly(), Is.True, "should inherit default settings key");
        });
    }

    [Test]
    public void EmptyBlockHandlerIsNotEvenBound()
    {
        // an empty {} block emits no config leaves, so the handler never enters options.Handlers
        // and is resolved purely from the defaults via the Clone() fallback.
        var set = ResolveSet(new()
        {
            [$"{SetPath}:HandlerDefaults:UseFlatStructure"] = "false",
            [$"{SetPath}:HandlerDefaults:GuidNames"] = "true",
            // note: no leaf keys under EmptyBlockHandler are possible from an empty object.
        });

        Assert.That(set.Handlers.ContainsKey("EmptyBlockHandler"), Is.False);

        var settings = set.GetHandlerSettings("EmptyBlockHandler");

        Assert.Multiple(() =>
        {
            Assert.That(settings.UseFlatStructure, Is.False);
            Assert.That(settings.GuidNames, Is.True);
        });
    }

    [Test]
    public void HandlerWithoutOwnBlockStillUsesDefaults()
    {
        var set = ResolveSet(new()
        {
            [$"{SetPath}:HandlerDefaults:UseFlatStructure"] = "false",
            [$"{SetPath}:HandlerDefaults:Settings:CreateOnly"] = "true",
        });

        var settings = set.GetHandlerSettings("SomeHandlerWithNoBlock");

        Assert.Multiple(() =>
        {
            Assert.That(settings.UseFlatStructure, Is.False);
            Assert.That(settings.IsCreateOnly(), Is.True);
        });
    }
}
