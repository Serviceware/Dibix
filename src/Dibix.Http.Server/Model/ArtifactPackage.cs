using System;
using System.IO;
using System.IO.Packaging;

namespace Dibix.Http.Server
{
    internal static class ArtifactPackage
    {
        public static byte[] GetContent(string packagePath)
        {
            using (Package package = Package.Open(packagePath, FileMode.Open))
            {
                Uri contentUri = new Uri("Content", UriKind.Relative);
                Uri partUri = PackUriHelper.CreatePartUri(contentUri);
                PackagePart part = package.GetPart(partUri);
                using (Stream inputStream = part.GetStream())
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        inputStream.CopyTo(outputStream);
                        return outputStream.ToArray();
                    }
                }
            }
        }
    }
}