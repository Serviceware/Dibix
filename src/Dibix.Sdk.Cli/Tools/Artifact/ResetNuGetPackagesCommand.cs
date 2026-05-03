using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli.Tools
{
    internal sealed class ResetNuGetPackagesCommand : ValidatableActionCommand
    {
        private readonly EnvironmentVariableOption _consumerDirectoryOption;
        private readonly Argument<string> _packageNameArgument;
        private ConsumerPackageManager _consumerPackageManager;

        public ResetNuGetPackagesCommand(EnvironmentVariableOption consumerDirectoryOption) : base("reset", "Removes the current version of the Dibix packages from the local NuGet package cache and reverts the consumer to the previous version.")
        {
            _consumerDirectoryOption = consumerDirectoryOption;
            _packageNameArgument = new Argument<string>("package-name")
            {
                Description = "The name of the package to reset. If not specified, all packages will be reset.",
                Arity = ArgumentArity.ZeroOrOne
            };

            Add(_packageNameArgument);
        }

        protected override void Validate(CommandResult commandResult)
        {
            bool loggedMessages = false;
            string consumerDirectory = _consumerDirectoryOption.CollectValue(commandResult, isRequired: true, ref loggedMessages);

            if (consumerDirectory != null)
                _consumerPackageManager = PackageUtility.GetPackageManagerForConsumer(consumerDirectory);

            if (loggedMessages)
                Console.WriteLine();
        }

        protected override async Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            if (_consumerPackageManager == null)
                throw new InvalidOperationException("Consumer package manager not initialized");

            string packageToReset = parseResult.GetValue(_packageNameArgument);
            ConsoleUtility.WriteLineInformation(packageToReset == null ? "Resetting all packages.." : $"Resetting only package '{packageToReset}'..");

            string[] packagesToReset = packageToReset != null ? [packageToReset] : PackageUtility.NuGetPackageNames;

            foreach (string packageName in packagesToReset)
            {
                string packageVersion = await _consumerPackageManager.GetPackageVersion(packageName).ConfigureAwait(false);

                ConsoleUtility.WriteLineDebug($"Removing package '{packageName}' version '{packageVersion}' from local NuGet package cache..");
                DirectoryInfo cacheDirectory = new DirectoryInfo(Path.Combine(PackageUtility.PackageCacheDirectory, packageName, packageVersion));
                if (cacheDirectory.Exists)
                    cacheDirectory.Delete(recursive: true);
            }

            return 0;
        }
    }
}