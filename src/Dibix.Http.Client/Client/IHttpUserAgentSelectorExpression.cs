using System;
using System.Reflection;

namespace Dibix.Http.Client
{
    public interface IHttpUserAgentSelectorExpression
    {
        void FromAssembly(Assembly assembly, Func<string, string> productNameFormatter = null);
        void FromEntryAssembly(Func<string, string> productNameFormatter = null);
    }
}