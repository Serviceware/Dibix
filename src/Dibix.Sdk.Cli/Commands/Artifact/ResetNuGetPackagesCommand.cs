using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal sealed class ResetNuGetPackagesCommand : ConsumerPackageCommand
    {
        private readonly Argument<string> _packageNameArgument;
        public ResetNuGetPackagesCommand(EnvironmentVariableOption consumerDirectoryOption) : base("reset", "Removes the current version of the Dibix packages from the local NuGet package cache and reverts the consumer to the previous version.", consumerDirectoryOption)
        {
            _packageNameArgument = new Argument<string>("package-name")
            {
                Description = "The name of the package to reset. If not specified, all packages will be reset.",
                Arity = ArgumentArity.ZeroOrOne
            };

            Add(_packageNameArgument);
        }

        protected override async Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            if (ConsumerPackageManager == null)
                throw new InvalidOperationException("Consumer package manager not initialized");

            string packageToReset = parseResult.GetValue(_packageNameArgument);
            ConsoleUtility.WriteLineInformation(packageToReset == null ? "Resetting all packages.." : $"Resetting only package '{packageToReset}'");

            string[] packagesToReset = packageToReset != null ? [packageToReset] : PackageUtility.NuGetPackageNames;

            foreach (string packageName in packagesToReset)
            {
                string packageVersion = await ConsumerPackageManager.GetPackageVersion(packageName).ConfigureAwait(false);

                ConsoleUtility.WriteLineDebug($"Removing package '{packageName}' version '{packageVersion}' from local NuGet package cache");
                PackageUtility.RemovePackageFromNuGetPackageCache(packageName, packageVersion);

                ConsoleUtility.WriteLineDebug($"Reverting consumer package reference of package '{packageName}'");
                await ConsumerPackageManager.RevertPackageVersionChanges(packageName, PackageUtility.IsSdk(packageName), cancellationToken).ConfigureAwait(false);
            }

            return 0;
        }
    }
}