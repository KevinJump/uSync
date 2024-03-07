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
}
