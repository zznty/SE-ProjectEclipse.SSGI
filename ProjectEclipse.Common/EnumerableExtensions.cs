using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEclipse.Common
{
    public static class EnumerableExtensions
    {
        public static void DisposeAll<T>(this IEnumerable<T> collection) where T : IDisposable
        {
            foreach (var item in collection)
            {
                item?.Dispose();
            }
        }
    }
}
