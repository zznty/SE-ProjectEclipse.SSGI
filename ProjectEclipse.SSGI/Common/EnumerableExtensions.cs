using System;
using System.Collections.Generic;

namespace ProjectEclipse.SSGI.Common
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
