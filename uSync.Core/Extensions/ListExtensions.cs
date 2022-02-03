using System;
using System.Collections.Generic;
using System.Linq;

namespace uSync.Core
{
    public static class ListExtensions
    {
        /// <summary>
        /// Add item to list if the item is not null
        /// </summary>
        public static void AddNotNull<TObject>(this List<TObject> list, TObject item)
        {
            if (item == null) return;
            list.Add(item);
        }

        /// <summary>
        ///  Is the value valid for this list (if the list is empty, we treat it like a wildcard).
        /// </summary>
        public static bool IsValid(this IList<string> list, string value)
            => list.Count == 0 || Umbraco.Extensions.StringExtensions.InvariantContains(list, value) || 
            Umbraco.Extensions.StringExtensions.InvariantContains(list, "*");

        public static bool IsValidOrBlank(this IList<string> list, string value)
            => string.IsNullOrWhiteSpace(value) || list.IsValid(value);


        public static IEnumerable<TSource> SafeDistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
#if NET6_0_OR_GREATER
            return source.DistinctBy(keySelector);
#else
            return Umbraco.Extensions.EnumerableExtensions.DistinctBy(source, keySelector);
#endif
        }
    }
}
