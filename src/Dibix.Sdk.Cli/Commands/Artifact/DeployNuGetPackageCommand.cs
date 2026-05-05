using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal sealed class DeployNuGetPackageCommand : ConsumerPackageCommand
    {
        private readonly Argument<string> _packageNameArgument;
        private readonly Option<string> _configurationOption;

        public DeployNuGetPackageCommand(EnvironmentVariableOption consumerDirectoryOption) : base("deploy", "Creates NuGet package(s), deploys them to the local NuGet package cache and updates the package version(s) at the consumer.", consumerDirectoryOption)
        {
            _packageNameArgument = new Argument<string>("package-name")
            {
                Description = "The name of the package to deploy. If not specified, all packages will be deployed.",
                Arity = ArgumentArity.ZeroOrOne
            };
            _configurationOption = new Option<string>("--configuration", "-c")
            {
                Description = "The build configuration to use when creating the NuGet package(s).",
                DefaultValueFactory = x => "Release"
            };

            Add(_packageNameArgument);
            Add(_configurationOption);
        }

        protected override async Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            if (ConsumerPackageManager == null)
                throw new InvalidOperationException("Consumer package manager not initialized");

            string packageToDeploy = parseResult.GetValue(_packageNameArgument);
            ConsoleUtility.WriteLineInformation(packageToDeploy == null ? "Deploying all packages" : $"Deploying only package '{packageToDeploy}'");

            string[] packagesToDeploy = packageToDeploy != null ? [packageToDeploy] : PackageUtility.NuGetPackageNames;
            string localDibixVersion = await PackageUtility.GetLocalDibixVersion().ConfigureAwait(false);
            string configuration = parseResult.GetRequiredValue(_configurationOption);

            ConsoleUtility.WriteLineDebug($"Local Dibix version: {localDibixVersion}");

            foreach (string packageName in packagesToDeploy)
            {
                ConsoleUtility.WriteLineInformation(packageName);

                ConsoleUtility.WriteLineDebug($"Creating NuGet package for '{packageName}'");
                await PackageUtility.CreateNuGetPackage(packageName, localDibixVersion, configuration).ConfigureAwait(false);

                ConsoleUtility.WriteLineDebug($"Removing package '{packageName}' version '{localDibixVersion}' from local NuGet package cache");
                PackageUtility.RemovePackageFromNuGetPackageCache(packageName, localDibixVersion);

                ConsoleUtility.WriteLineDebug($"Deploying package '{packageName}' version '{localDibixVersion}' to local NuGet package cache");
                PackageUtility.DeployPackageToNuGetPackageCache(packageName, localDibixVersion, configuration);

                string consumerPackageVersion = await ConsumerPackageManager.GetPackageVersion(packageName).ConfigureAwait(false);
                if (localDibixVersion != consumerPackageVersion)
                {
                    ConsoleUtility.WriteLineDebug($"Updating consumer package reference of package '{packageName}' from version '{consumerPackageVersion}' to '{localDibixVersion}'");
                    await ConsumerPackageManager.SetPackageVersionMSBuild(packageName, localDibixVersion, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    ConsoleUtility.WriteLineDebug($"Consumer already uses package '{packageName}' with version '{localDibixVersion}'");
                }
                if (PackageUtility.IsSdk(packageName))
                {
                    await ConsumerPackageManager.SetPackageVersionGlobalJson(packageName, localDibixVersion).ConfigureAwait(false);
                }

            }

            return 0;
        }
    }
}