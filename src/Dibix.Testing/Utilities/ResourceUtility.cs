using System;
using System.IO;
using System.Reflection;

namespace Dibix.Testing
{
    internal static class ResourceUtility
    {
        public static string GetEmbeddedResourceContent(Assembly assembly, string resourceKey)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceKey))
            {
                if (stream == null)
                    throw new InvalidOperationException($@"Resource not found: {resourceKey}
{assembly.Location}");

                using (TextReader reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    return content;
                }
            }
        }
    }
}