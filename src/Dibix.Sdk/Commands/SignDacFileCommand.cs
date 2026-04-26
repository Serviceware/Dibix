using System.Threading.Tasks;
using Dibix.Sdk.Sql;

namespace Dibix.Sdk
{
    [CommandLineAction("sign", "Appends Dibix metadata to an existing DAC file.")]
    [CommandLineInputProperty("DacFilePath", CommandLineInputPropertyType.String)]
    [CommandLineInputProperty("IsEmbedded", CommandLineInputPropertyType.Boolean)]
    [CommandLineInputProperty("LockRetryCount", CommandLineInputPropertyType.Int32)]
    public sealed partial class SignDacFileCommand
    {
        public partial Task<int> Execute(SignDacFileCommandInput input)
        {
            DacMetadataManager.SetIsEmbedded(input.DacFilePath, input.IsEmbedded, input.LockRetryCount, _logger.LogMessage);
            return Task.FromResult(0);
        }
    }
}