using System.Reflection;

using Umbraco.Cms.Core.Models.Membership;

namespace uSync.Core.Extensions;
internal static class DictionaryExtensions
{
    /// <summary>
    ///  get the username
    /// </summary>
    /// <param name="usernames"></param>
    /// <param name="id"></param>
    /// <param name="findMethod"></param>
    public static string GetUsername(this Dictionary<int, string> usernames, int? id, Func<int, IUser> findMethod)
    {
        if (usernames == null || id == null) return "unknown";

        if (usernames.TryGetValue(id.Value, out string? username))
            return username;

        return usernames[id.Value] = findMethod(id.Value)?.Email ?? "unknown";
    }

    public static int GetEmails(this Dictionary<string, int> emails, string email, Func<string, IUser> findMethod)
    {
        if (emails == null || string.IsNullOrEmpty(email)) return -1;

        if (emails.TryGetValue(email, out int value))
            return value;

        return emails[email] = findMethod(email)?.Id ?? -1;
    }

    // This method converts an object to a dictionary of string, object
    public static IDictionary<string, object?> ToKeyNameDictionary(this object obj)
    {
        // Get the public properties of the object
        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Create a dictionary and populate it with the property names and values
        Dictionary<string, object?> dictionary = new(properties.Length);
        foreach (var property in properties)
        {
            dictionary.Add(property.Name, property.GetValue(obj));
        }

        // Return the dictionary
        return dictionary;
    }

    public static bool TryConvertToDictionary(this object obj, out IDictionary<string, object> result)
    {
        result = new Dictionary<string, object>();

        if (obj == null) return false;

        try
        {
            var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Dictionary<string, object?> dictionary = new(properties.Length);
            foreach (var property in properties)
            {
                dictionary.Add(property.Name, property.GetValue(obj));
            }

            return true;
        }
        catch
        {
            return false;
        }

    }

    /// <summary>
    ///  merge two or more dictionaries together, throwing away duplicates!
    /// </summary>
    public static IDictionary<TKey, TValue> MergeIgnoreDuplicates<TKey, TValue>(this IDictionary<TKey, TValue>? source, params IDictionary<TKey, TValue>[] dictionaries)
        where TKey : notnull
    {
        var mergedDictionary = new Dictionary<TKey, TValue>(source?.ToDictionary() ?? []);

        foreach (var dictionary in dictionaries.Where(x => x is not null))
        {
            foreach (var kvp in dictionary)
            {
                mergedDictionary.TryAdd(kvp.Key, kvp.Value);
            }
        }

        return mergedDictionary;
    }

    public static IDictionary<string, TValue> ConvertToCamelCase<TValue>(this IDictionary<string, TValue> originalDictionary)
        => originalDictionary
            .ToDictionary(kvp => kvp.Key.ToCamelCase(), kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

    public static string ToCamelCase(this string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return string.Empty;
        }

        if (s.Length == 1)
        {
            return s.ToLowerInvariant();
        }

        return $"{char.ToLowerInvariant(s[0])}{s.AsSpan(1)}";
    }
}
