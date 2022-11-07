using System;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk
{
    [Task("dumplockfile")]
    public sealed partial class DumpLockFileTask
    {
        private partial bool Execute()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length < 4)
                return false;

            string lockFilePath = args[2];
            string dumpFilePath = args[3];
            using (LockEntryManager lockEntryManager = LockEntryManager.Create(reset: false, lockFilePath))
            {
                lockEntryManager.Write(dumpFilePath, encoded: false);
                return true;
            }
        }
    }
}