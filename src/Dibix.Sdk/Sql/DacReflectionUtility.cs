using System.Reflection;
using Microsoft.Data.Tools.Schema.Extensibility;

namespace Dibix.Sdk.Sql
{
    internal static class DacReflectionUtility
    {
        public static readonly Assembly SchemaSqlAssembly = typeof(IExtension).Assembly;
    }
}