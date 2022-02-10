using Dibix.Sdk.Sql;

namespace Dibix.Sdk.Cli
{
    [TaskRunner("dumplockfile")]
    internal sealed class DumpLockFileTaskRunner : TaskRunner
    {
        public DumpLockFileTaskRunner(ILogger logger) : base(logger) { }

        protected override bool Execute(string[] args)
        {
            if (args.Length < 2)
                return false;

            return DumpLockFileTask.Execute(path: args[1]);
        }
    }
}