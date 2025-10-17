using System.Threading.Tasks;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Packaging;

namespace Dibix.Sdk
{
    [Task("pack")]
    [TaskProperty("ProductName", TaskPropertyType.String)]
    [TaskProperty("AreaName", TaskPropertyType.String)]
    [TaskProperty("OutputDirectory", TaskPropertyType.String)]
    [TaskProperty("ArtifactTargetFileName", TaskPropertyType.String)]
    [TaskProperty("PackageMetadataFileName", TaskPropertyType.String)]
    [TaskProperty("CompiledArtifactFileName", TaskPropertyType.String)]
    public sealed partial class CreatePackageTask
    {
        private partial Task<bool> Execute()
        {
            ArtifactPackage.Create(_configuration);
            return Task.FromResult(true);
        }
    }
}