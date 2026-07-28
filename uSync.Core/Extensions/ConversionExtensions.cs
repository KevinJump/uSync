namespace uSync.Core.Extensions;
internal static class ConversionExtensions
{
    public static TObject? GetValueAs<TObject>(this object value)
    {
        if (value == null) return default;

        // fully qualified, because this file lives in the same namespace as the obsolete
        // JsonTextExtensions shim - which would win on namespace proximity over a using.
        return Jumoo.Json.JsonSerialization.TryGetValueAs<TObject>(value, out var result)
            ? result : default;
    }

    public static Guid ConvertToGuid(this int value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, value);
        return new Guid(bytes);
    }
}
