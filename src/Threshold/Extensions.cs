using System;
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
    }
}
