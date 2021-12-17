using Dibix.Sdk.Packaging;

namespace Dibix.Sdk
{
    public static class CreatePackageTask
    {
        public static bool Execute
        (
            string artifactName
          , string outputDirectory
          , string compiledArtifactPath
        )
        {
            ArtifactPackage.Create(artifactName, outputDirectory, compiledArtifactPath);
            return true;
        }
    }
}