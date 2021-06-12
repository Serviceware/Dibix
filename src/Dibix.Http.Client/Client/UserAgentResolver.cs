using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;

namespace Dibix.Http.Client
{
    internal static class UserAgentResolver
    {
        public static ProductInfoHeaderValue FromAssembly(Assembly assembly, Func<string, string> productNameFormatter = null) => ResolveUserAgentFromAssembly(assembly, productNameFormatter);

        public static ProductInfoHeaderValue FromEntryAssembly(Func<string, string> productNameFormatter = null) => ResolveUserAgentFromAssembly(ResolveEntryAssembly(), productNameFormatter);

        private static ProductInfoHeaderValue ResolveUserAgentFromAssembly(Assembly assembly, Func<string, string> productNameFormatter)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string productName = fileVersionInfo.ProductName;
            string assemblyName = Path.GetFileNameWithoutExtension(assembly.Location);
            string productVersion = fileVersionInfo.ProductVersion;

            string userAgentProductName = $"{productName}{assemblyName}";
            if (productNameFormatter != null)
                userAgentProductName = productNameFormatter(userAgentProductName);

            return new ProductInfoHeaderValue(userAgentProductName, productVersion);
        }

        private static Assembly ResolveEntryAssembly()
        {
            Assembly assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
                throw new InvalidOperationException("Could not determine entry assembly");

            return assembly;
        }
    }
}