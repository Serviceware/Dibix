namespace Dibix.Sdk.Sql
{
    public static class SignDacFileTask
    { 
        public static bool Execute(string dacFilePath, bool isEmbedded, int lockRetryCount, ILogger logger)
        {
            DacMetadataManager.SetIsEmbedded(dacFilePath, isEmbedded, lockRetryCount, logger.LogMessage);
            return true;
        }
    }
}