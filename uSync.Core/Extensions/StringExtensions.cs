using Umbraco.Extensions;

namespace uSync.Core;

public static class StringExtensions
{

    /// <summary>
    ///  things can't be called web.config or app.config it causes issues on build and publish 
    /// </summary>
    private static readonly string[] BadNames = new[]
    {
        "app.config", "web.config"
    };

    public static string ToAppSafeFileName(this string value)
    {
        var filename = Path.GetFileName(value);
        if (BadNames.InvariantContains(filename))
        {
            return Path.Combine(
                Path.GetDirectoryName(value),
                $"__{Path.GetFileNameWithoutExtension(value)}__{Path.GetExtension(value)}");
        }
        return value;
    }


    private static readonly char[] Base32Table =
    {
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
        'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5'
    };

    /// IN UmbracoCore but private (so renamed, to avoid future clashes)
    /// <summary>
    /// Converts a Guid into a base-32 string.
    /// </summary>
    /// <param name="guid">A Guid.</param>
    /// <param name="length">The string length.</param>
    /// <returns>A base-32 encoded string.</returns>
    /// <remarks>
    /// <para>A base-32 string representation of a Guid is the shortest, efficient, representation
    /// that is case insensitive (base-64 is case sensitive).</para>
    /// <para>Length must be 1-26, anything else becomes 26.</para>
    /// </remarks>
    public static string ToShortKeyString(this Guid guid, int length = 26)
    {
        if (length <= 0 || length > 26)
            length = 26;

        var bytes = guid.ToByteArray(); // a Guid is 128 bits ie 16 bytes

        // this could be optimized by making it unsafe,
        // and fixing the table + bytes + chars (see Convert.ToBase64CharArray)

        // each block of 5 bytes = 5*8 = 40 bits
        // becomes 40 bits = 8*5 = 8 byte-32 chars
        // a Guid is 3 blocks + 8 bits

        // so it turns into a 3*8+2 = 26 chars string
        var chars = new char[length];

        var i = 0;
        var j = 0;

        while (i < 15)
        {
            if (j == length) break;
            chars[j++] = Base32Table[(bytes[i] & 0b1111_1000) >> 3];
            if (j == length) break;
            chars[j++] = Base32Table[((bytes[i] & 0b0000_0111) << 2) | ((bytes[i + 1] & 0b1100_0000) >> 6)];
            if (j == length) break;
            chars[j++] = Base32Table[(bytes[i + 1] & 0b0011_1110) >> 1];
            if (j == length) break;
            chars[j++] = Base32Table[(bytes[i + 1] & 0b0000_0001) | ((bytes[i + 2] & 0b1111_0000) >> 4)];
            if (j == length) break;
            chars[j++] = Base32Table[((bytes[i + 2] & 0b0000_1111) << 1) | ((bytes[i + 3] & 0b1000_0000) >> 7)];
            if (j == length) break;
            chars[j++] = Base32Table[(bytes[i + 3] & 0b0111_1100) >> 2];
            if (j == length) break;
            chars[j++] = Base32Table[((bytes[i + 3] & 0b0000_0011) << 3) | ((bytes[i + 4] & 0b1110_0000) >> 5)];
            if (j == length) break;
            chars[j++] = Base32Table[bytes[i + 4] & 0b0001_1111];

            i += 5;
        }

        if (j < length)
            chars[j++] = Base32Table[(bytes[i] & 0b1111_1000) >> 3];
        if (j < length)
            chars[j] = Base32Table[(bytes[i] & 0b0000_0111) << 2];

        return new string(chars);
    }


    public static int GetDeterministicHashCode(this string str)
    {
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1)
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }

    /// <summary>
    ///  Is the object a string is it null or blank ?
    /// </summary>
    public static bool IsObjectNullOrEmptyString(this object value)
        => value == null || (value is string valueString && string.IsNullOrWhiteSpace(valueString));

}
