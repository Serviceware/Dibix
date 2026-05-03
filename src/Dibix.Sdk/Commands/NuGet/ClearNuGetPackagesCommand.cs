using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk
{
    public sealed class ClearNuGetPackagesCommand : ActionCommand
    {
        public ClearNuGetPackagesCommand() : base("clear", "Remove all local Dibix NuGet package versions from cache.")
        {
        }

        protected override Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            WriteLineInformation("Clearing dibix packages from NuGet cache directory..");
            foreach (DirectoryInfo directory in new DirectoryInfo(NuGetUtility.PackageCacheDirectory).GetDirectories("dibix*"))
            {
                WriteLineDebug(directory.FullName);
                directory.Delete(recursive: true);
            }

            return Task.FromResult(0);
        }
    }
}