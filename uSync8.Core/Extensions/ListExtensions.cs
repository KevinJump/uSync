using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace uSync8.Core.Extensions
{
    public static class ListExtensions
    {
        public static void AddNotNull<TObject>(this List<TObject> list, TObject item)
        {
            if (item == null) return;
            list.Add(item);
        }
    }
}
