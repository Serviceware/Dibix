using System.Threading.Tasks;

namespace Dibix.Sdk
{
    [CommandLineAction("dumplockfile", "Reads the given binary lock file and writes it to a human-readable json file.")]
    [CommandLineActionArgument("lockFilePath", typeof(string), "Path to the binary lock file.")]
    [CommandLineActionArgument("dumpFilePath", typeof(string), "Path to the target JSON file.")]
    public sealed partial class DumpLockFileCommand
    {
        public partial Task<int> Execute(string lockFilePath, string dumpFilePath)
        {
            using LockEntryManager lockEntryManager = LockEntryManager.Create(reset: false, lockFilePath);
            lockEntryManager.Write(dumpFilePath, encoded: false);
            return Task.FromResult(0);
        }
    }
}