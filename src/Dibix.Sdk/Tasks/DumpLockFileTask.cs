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
            if (args.Length < 2)
                return false;

            using (LockEntryManager lockEntryManager = LockEntryManager.Create())
            {
                lockEntryManager.Write(args[1], encoded: false);
                return true;
            }
        }
    }
}