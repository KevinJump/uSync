using System;
using System.Text.Json;

using NUnit.Framework;

using uSync.Core.Extensions;

namespace uSync.Tests.Extensions;

/// <summary>
///  tests for the JsonElement pre-check that avoids the swallowed
///  InvalidCastException Umbraco's TryConvertTo throws on JsonElement values
///  (uSync.Complete issue #304).
/// </summary>
[TestFixture]
internal class TryGetValueAsTests
{
    [Test]
    public void JsonElementTrue_ConvertsToBool()
    {
        object value = JsonSerializer.SerializeToElement(true);

        var success = value.TryGetValueAs<bool>(out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.True);
        });
    }

    [Test]
    public void JsonElementFalse_ConvertsToBool()
    {
        object value = JsonSerializer.SerializeToElement(false);

        var success = value.TryGetValueAs<bool>(out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.False);
        });
    }

    [Test]
    public void JsonElementNumber_ConvertsToInt()
    {
        object value = JsonSerializer.SerializeToElement(42);

        var success = value.TryGetValueAs<int>(out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(42));
        });
    }

    [Test]
    public void JsonElementString_ConvertsToGuid()
    {
        var guid = Guid.NewGuid();
        object value = JsonSerializer.SerializeToElement(guid.ToString());

        var success = value.TryGetValueAs<Guid>(out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(guid));
        });
    }

    [Test]
    public void JsonElementString_ConvertsToString()
    {
        object value = JsonSerializer.SerializeToElement("hello");

        var success = value.TryGetValueAs<string>(out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("hello"));
        });
    }

    // a plain CLR value skips the JsonElement branch and still converts via
    // the TryConvertTo fallback - behaviour must be unchanged for these.
    [Test]
    public void PlainString_ConvertsToInt_ViaFallback()
    {
        object value = "42";

        var success = value.TryGetValueAs<int>(out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(42));
        });
    }

    [Test]
    public void Null_ReturnsFalse()
    {
        object? value = null;

        var success = value.TryGetValueAs<bool>(out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.False);
        });
    }
}
