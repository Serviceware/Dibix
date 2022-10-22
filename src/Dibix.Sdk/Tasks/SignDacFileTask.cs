using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;

namespace Dibix.Sdk
{
    [Task("sign")]
    [TaskProperty("DacFilePath", TaskPropertyType.String)]
    [TaskProperty("IsEmbedded", TaskPropertyType.Boolean)]
    [TaskProperty("LockRetryCount", TaskPropertyType.Int32)]
    public sealed partial class SignDacFileTask
    { 
        private partial bool Execute()
        {
            DacMetadataManager.SetIsEmbedded(_configuration.DacFilePath, _configuration.IsEmbedded, _configuration.LockRetryCount, _logger.LogMessage);
            return true;
        }
    }
}