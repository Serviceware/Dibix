using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Dibix.Http.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dibix.Testing.Http
{
    public static class HttpClientExtensions
    {
        public static HttpClient AddUserAgentFromTestAssembly(this HttpClient client, Func<string, string> productNameFormatter = null) => client.AddUserAgentFromAssembly(ResolveTestAssembly(), productName =>
        {
            string normalizedProductName = productName.Replace(".", null);

            if (productNameFormatter != null)
                normalizedProductName = productNameFormatter(normalizedProductName);

            return normalizedProductName;
        });

        private static Assembly ResolveTestAssembly()
        {
            Assembly assembly = new StackTrace().GetFrames()
                                                .Select(x => x.GetMethod())
                                                .Where(x => (x?.IsDefined(typeof(TestMethodAttribute))).GetValueOrDefault(false))
                                                .Select(x => x.DeclaringType?.Assembly)
                                                .FirstOrDefault();

            if (assembly == null)
                throw new InvalidOperationException("Could not determine test assembly");

            return assembly;
        }
    }
}