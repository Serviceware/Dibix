using System;
using System.Reflection;

namespace Dibix.Http.Client
{
    public interface IHttpUserAgentSelectorExpression
    {
        void FromAssembly(Assembly assembly, Func<string, string> productNameFormatter = null);
        void FromAssemblyContainingType<T>(Func<string, string> productNameFormatter = null);
        void FromEntryAssembly(Func<string, string> productNameFormatter = null);
        void FromCurrentProcess(Func<string, string> productNameFormatter = null);
        void FromFile(string path, Func<string, string> productNameFormatter = null);
    }
}