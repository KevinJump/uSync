using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;

namespace uSync8.Core
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
        ///  Is the value valid for this list (if the list is empty, we say the value is valid).
        /// </summary>
        public static bool IsValid(this IList<string> list, string value)
            => list.Count == 0 || list.InvariantContains(value) || list.InvariantContains("*");
    }
}
