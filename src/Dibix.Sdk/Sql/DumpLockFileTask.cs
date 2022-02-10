namespace Dibix.Sdk.Sql
{
    public static class DumpLockFileTask
    { 
        public static bool Execute(string path)
        {
            using (LockEntryManager lockEntryManager = LockEntryManager.Create())
            {
                lockEntryManager.Write(path, encoded: false);
                return true;
            }
        }
    }
}