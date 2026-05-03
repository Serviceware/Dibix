using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class ClearNuGetPackagesCommand : ValidatableActionCommand
    {
        public ClearNuGetPackagesCommand() : base("clear", "Remove all Dibix NuGet package versions from the local NuGet package cache.")
        {
        }

        protected override Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            ConsoleUtility.WriteLineInformation("Clearing dibix packages from NuGet cache directory..");
            foreach (DirectoryInfo directory in new DirectoryInfo(PackageUtility.PackageCacheDirectory).GetDirectories("dibix*"))
            {
                ConsoleUtility.WriteLineDebug(directory.FullName);
                directory.Delete(recursive: true);
            }

            return Task.FromResult(0);
        }
    }
}