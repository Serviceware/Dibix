using System.Reflection;

namespace Dibix.Generators.Tests
{
    internal static class TestUtility
    {
        public static readonly string GeneratorFileVersion = Assembly.Load("Dibix.Generators").GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
    }
}