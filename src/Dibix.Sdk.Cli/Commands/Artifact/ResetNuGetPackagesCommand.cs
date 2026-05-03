using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal sealed class ResetNuGetPackagesCommand : ConsumerPackageCommand
    {
        protected override string PackageNameArgumentDescription => "The name of the package to reset. If not specified, all packages will be reset.";

        public ResetNuGetPackagesCommand(EnvironmentVariableOption consumerDirectoryOption) : base("reset", "Removes the current version of the Dibix packages from the local NuGet package cache and reverts the consumer to the previous version.", consumerDirectoryOption)
        {
        }

        protected override async Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            if (ConsumerPackageManager == null)
                throw new InvalidOperationException("Consumer package manager not initialized");

            ConsoleUtility.WriteLineInformation(PackageName == null ? "Resetting all packages.." : $"Resetting only package '{PackageName}'");

            string[] packagesToReset = PackageName != null ? [PackageName] : ArtifactUtility.NuGetPackageNames;

            foreach (string packageName in packagesToReset)
            {
                string packageVersion = await ConsumerPackageManager.GetPackageVersion(packageName, cancellationToken).ConfigureAwait(false);

                ConsoleUtility.WriteLineDebug($"Removing package '{packageName}' version '{packageVersion}' from local NuGet package cache");
                ArtifactUtility.RemovePackageFromNuGetPackageCache(packageName, packageVersion);

                ConsoleUtility.WriteLineDebug($"Reverting consumer package reference of package '{packageName}'");
                await ConsumerPackageManager.RevertPackageVersionChanges(packageName, ArtifactUtility.IsSdk(packageName), cancellationToken).ConfigureAwait(false);
            }

            return 0;
        }
    }
}