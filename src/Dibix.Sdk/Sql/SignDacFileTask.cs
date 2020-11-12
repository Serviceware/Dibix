namespace Dibix.Sdk.Sql
{
    public static class SignDacFileTask
    { 
        public static bool Execute(string dacFilePath, bool isEmbedded)
        {
            DacMetadataManager.SetIsEmbedded(dacFilePath, isEmbedded);
            return true;
        }
    }
}