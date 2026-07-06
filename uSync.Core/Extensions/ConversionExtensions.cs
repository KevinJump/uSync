using Umbraco.Extensions;

namespace uSync.Core.Extensions;
internal static class ConversionExtensions
{
    public static TObject? GetValueAs<TObject>(this object value)
    {
        if (value == null) return default;
        var attempt = value.TryConvertTo<TObject>();
        if (!attempt) return default;
        return attempt.Result;
    }

    public static Guid ConvertToGuid(this int value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes);
        return new Guid(bytes);
    }
}
