using System;
using System.IO;
using System.IO.Packaging;

namespace Dibix.Sdk.Packaging
{
    internal static class ArtifactPackage
    {
        public static void Create(CreatePackageTaskConfiguration configuration)
        {
            string targetPackagePath = Path.Combine(configuration.OutputDirectory, configuration.ArtifactTargetFileName);
            string compiledArtifactPath = Path.Combine(configuration.OutputDirectory, configuration.CompiledArtifactFileName);
            string packageMetadataPath = Path.Combine(configuration.OutputDirectory, configuration.PackageMetadataFileName);
            Create(targetPackagePath, compiledArtifactPath, packageMetadataPath, configuration.ProductName, configuration.AreaName);
        }
        private static void Create(string targetPackagePath, string compiledArtifactPath, string packageMetadataPath, string productName, string areaName)
        {
            using Package package = Package.Open(targetPackagePath, FileMode.Create);
            AppendPackageProperties(package, productName, areaName);
            AppendPart(package, "Metadata", packageMetadataPath);
            AppendPart(package, "Content", compiledArtifactPath);
        }

        private static void AppendPackageProperties(Package package, string productName, string areaName)
        {
            package.PackageProperties.Title = productName;
            package.PackageProperties.Subject = areaName;
        }

        private static void AppendPart(Package package, string partName, string sourceFilePath)
        {
            Uri contentUri = PackUriHelper.CreatePartUri(new Uri(partName, UriKind.Relative));
            PackagePart contentPart = package.CreatePart(contentUri, MimeTypes.GetMimeType(sourceFilePath));
            Stream content = File.OpenRead(sourceFilePath);
            using Stream contentInput = content;
            using Stream contentOutput = contentPart.GetStream();
            content.CopyTo(contentOutput);
        }
    }
}