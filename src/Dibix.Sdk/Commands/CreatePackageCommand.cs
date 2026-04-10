using System.Threading.Tasks;
using Dibix.Sdk.Packaging;

namespace Dibix.Sdk
{
    [CommandLineAction("pack", "Creates a Dibix .dbx file containing endpoints and dependent artifacts.")]
    [CommandLineInputProperty("ProductName", CommandLineInputPropertyType.String)]
    [CommandLineInputProperty("AreaName", CommandLineInputPropertyType.String)]
    [CommandLineInputProperty("OutputDirectory", CommandLineInputPropertyType.String)]
    [CommandLineInputProperty("ArtifactTargetFileName", CommandLineInputPropertyType.String)]
    [CommandLineInputProperty("PackageMetadataFileName", CommandLineInputPropertyType.String)]
    [CommandLineInputProperty("CompiledArtifactFileName", CommandLineInputPropertyType.String)]
    public sealed partial class CreatePackageCommand
    {
        public partial Task<int> Execute(CreatePackageCommandInput input)
        {
            ArtifactPackage.Create(input);
            return Task.FromResult(0);
        }
    }
}