namespace Dibix.Sdk.Tests
{
    internal static class ResourceUtility
    {
        private static readonly string AssemblyName = typeof(ResourceUtility).Assembly.GetName().Name;

        public static string BuildResourceKey(string relativeKey)
        {
            string resourceKey = $"{AssemblyName}.Resources.{relativeKey}";
            return resourceKey;
        }
    }
}