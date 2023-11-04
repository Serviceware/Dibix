using System;
using System.IO;
using System.IO.Packaging;

namespace Dibix.Sdk.Packaging
{
    internal static class ArtifactPackage
    {
        public static void Create(CreatePackageTaskConfiguration configuration)
        {
            string outputPath = Path.Combine(configuration.OutputDirectory, configuration.ArtifactTargetFileName);
            using (Package package = Package.Open(outputPath, FileMode.Create))
            {
                Uri contentUri = PackUriHelper.CreatePartUri(new Uri("Content", UriKind.Relative));
                PackagePart contentPart = package.CreatePart(contentUri, "application/octet-stream");
                using (Stream contentInput = File.OpenRead(configuration.CompiledArtifactPath))
                {
                    using (Stream contentOutput = contentPart.GetStream())
                    {
                        contentInput.CopyTo(contentOutput);
                    }
                }
            }
        }
    }
}