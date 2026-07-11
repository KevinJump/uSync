using Umbraco.Extensions;

using uSync.Core.Extensions;

namespace uSync.Core;

public static class ListExtensions
{
    /// <summary>
    /// Add item to list if the item is not null
    /// </summary>
    public static void AddNotNull<TObject>(this List<TObject> list, TObject? item)
    {
        if (item is not null) list.Add(item);
    }

    public static void AddRangeIfNotNull<TObject>(this List<TObject>? list, IEnumerable<TObject>? items)
    {
        if (items is null) return;
        list?.AddRange(items);
    }

    /// <summary>
    ///  Is the value valid for this list (if the list is empty, we treat it like a wildcard).
    /// </summary>
    public static bool IsValid(this IList<string> list, string value)
        => list.Count == 0 || Umbraco.Extensions.StringExtensions.InvariantContains(list, value) ||
        Umbraco.Extensions.StringExtensions.InvariantContains(list, "*");

    public static bool IsValidOrBlank(this IList<string> list, string value)
        => string.IsNullOrWhiteSpace(value) || list.IsValid(value);

    /// <summary>
    /// Converts a list of strings to an enumerable of the specified type.
    /// Skips null, empty, or whitespace strings and only returns successfully converted items.
    /// </summary>
    /// <typeparam name="T">The target type to convert each string to</typeparam>
    /// <param name="items">The list of strings to convert</param>
    /// <returns>An enumerable containing only the successfully converted items of type T</returns>
    internal static IEnumerable<T> ConvertItems<T>(this IList<string> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            if (item.TryGetValueAs<T>(out var result))
            {
                yield return result;
            }
        }
    }
}
