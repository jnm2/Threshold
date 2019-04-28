using System;
using System.Collections.Generic;
using System.Reflection;

namespace Threshold
{
    internal static class Extensions
    {
        public static string GetProductNameAndVersion(this Assembly assembly)
        {
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));

            var productName = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                              ?? assembly.GetName().Name;

            var productVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                 ?? assembly.GetName().Version.ToString();

            return productName + ' ' + productVersion;
        }

        public static string GetCopyright(this Assembly assembly)
        {
            if (assembly is null) throw new ArgumentNullException(nameof(assembly));

            return assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        }

        public static T MinBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey> comparer = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

            using (var en = source.GetEnumerator())
            {
                if (!en.MoveNext()) throw new InvalidOperationException("Sequence contained no elements.");

                var minValue = en.Current;
                var minKey = keySelector.Invoke(minValue);

                if (comparer is null) comparer = Comparer<TKey>.Default;

                while (en.MoveNext())
                {
                    var current = en.Current;
                    var currentKey = keySelector.Invoke(current);

                    if (comparer.Compare(minKey, currentKey) > 0)
                    {
                        minValue = current;
                        minKey = currentKey;
                    }
                }

                return minValue;
            }
        }
    }
}
