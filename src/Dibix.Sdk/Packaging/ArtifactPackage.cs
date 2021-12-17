using System;
using System.IO;
using System.IO.Packaging;

namespace Dibix.Sdk.Packaging
{
    internal static class ArtifactPackage
    {
        public static void Create(string artifactName, string outputDirectory, string compiledArtifactPath)
        {
            string outputPath = Path.Combine(outputDirectory, $"{artifactName}.dbx");
            using (Package package = Package.Open(outputPath, FileMode.Create))
            {
                Uri contentUri = PackUriHelper.CreatePartUri(new Uri("Content", UriKind.Relative));
                PackagePart contentPart = package.CreatePart(contentUri, "application/octet-stream");
                using (Stream contentInput = File.OpenRead(compiledArtifactPath))
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