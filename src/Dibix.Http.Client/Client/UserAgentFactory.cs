using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;

namespace Dibix.Http.Client
{
    public static class UserAgentFactory
    {
        public static ProductInfoHeaderValue FromAssembly(Assembly assembly, Func<string, string> productNameFormatter = null) => ResolveUserAgentFromAssembly(assembly, productNameFormatter);

        public static ProductInfoHeaderValue FromAssemblyContainingType<T>(Func<string, string> productNameFormatter = null) => FromAssemblyContainingType(typeof(T), productNameFormatter);
        public static ProductInfoHeaderValue FromAssemblyContainingType(Type type, Func<string, string> productNameFormatter = null) => ResolveUserAgentFromAssembly(type.Assembly, productNameFormatter);

        public static ProductInfoHeaderValue FromEntryAssembly(Func<string, string> productNameFormatter = null) => ResolveUserAgentFromAssembly(ResolveEntryAssembly(), productNameFormatter);

        public static ProductInfoHeaderValue FromCurrentProcess(Func<string, string> productNameFormatter = null) => FromFile(ResolveCurrentProcessPath(), productNameFormatter);

        public static ProductInfoHeaderValue FromFile(string path, Func<string, string> productNameFormatter)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(path);
            string productName = fileVersionInfo.ProductName;
            string assemblyName = Path.GetFileNameWithoutExtension(path);
            string normalizedAssemblyName = assemblyName.Replace(".", null);
            string productVersion = fileVersionInfo.ProductVersion;

            string userAgentProductName = $"{productName}{normalizedAssemblyName}";
            if (productNameFormatter != null)
                userAgentProductName = productNameFormatter(userAgentProductName);

            return new ProductInfoHeaderValue(userAgentProductName, productVersion);
        }

#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
        private static ProductInfoHeaderValue ResolveUserAgentFromAssembly(Assembly assembly, Func<string, string> productNameFormatter) => FromFile(assembly.Location, productNameFormatter);
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file

        private static Assembly ResolveEntryAssembly()
        {
            Assembly assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
                throw new InvalidOperationException("Could not determine entry assembly");

            return assembly;
        }

        private static string ResolveCurrentProcessPath()
        {
            ProcessModule processModule = Process.GetCurrentProcess().MainModule;
            if (processModule == null)
                throw new InvalidOperationException("Could not determine main module from current process");

            string filePath = processModule.FileName;
            return filePath;
        }
    }
}