using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal sealed class DeployNuGetPackageCommand : ConsumerPackageCommand
    {
        private readonly Option<string> _configurationOption;

        protected override string PackageNameArgumentDescription => "The name of the package to deploy. If not specified, all packages will be deployed.";

        public DeployNuGetPackageCommand(EnvironmentVariableOption consumerDirectoryOption) : base("deploy", "Creates NuGet package(s), deploys them to the local NuGet package cache and updates the package version(s) at the consumer.", consumerDirectoryOption)
        {
            _configurationOption = new Option<string>("--configuration", "-c")
            {
                Description = "The build configuration to use when creating the NuGet package(s).",
                DefaultValueFactory = _ => "Release"
            };

            Add(_configurationOption);
        }

        protected override async Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            if (ConsumerPackageManager == null)
                throw new InvalidOperationException("Consumer package manager not initialized");

            ConsoleUtility.WriteLineInformation(PackageName == null ? "Deploying all packages" : $"Deploying only package '{PackageName}'");

            string[] packagesToDeploy = PackageName != null ? [PackageName] : ArtifactUtility.NuGetPackageNames;
            string localDibixVersion = await ArtifactUtility.GetLocalDibixNuGetPackageVersion(cancellationToken).ConfigureAwait(false);
            string configuration = parseResult.GetRequiredValue(_configurationOption);

            ConsoleUtility.WriteLineDebug($"Local Dibix version: {localDibixVersion}");

            ConsoleUtility.WriteLineDebug("Restoring NuGet packages");
            await ArtifactUtility.RestoreNuGetPackages(cancellationToken).ConfigureAwait(false);

            foreach (string packageName in packagesToDeploy)
            {
                ConsoleUtility.WriteLineInformation(packageName);

                ConsoleUtility.WriteLineDebug($"Creating NuGet package for '{packageName}'");
                await ArtifactUtility.CreateNuGetPackage(packageName, localDibixVersion, configuration, cancellationToken).ConfigureAwait(false);

                ConsoleUtility.WriteLineDebug($"Removing package '{packageName}' version '{localDibixVersion}' from local NuGet package cache");
                ArtifactUtility.RemovePackageFromNuGetPackageCache(packageName, localDibixVersion);

                ConsoleUtility.WriteLineDebug($"Deploying package '{packageName}' version '{localDibixVersion}' to local NuGet package cache");
                ArtifactUtility.DeployPackageToNuGetPackageCache(packageName, localDibixVersion, configuration);

                string consumerPackageVersion = await ConsumerPackageManager.GetPackageVersion(packageName, cancellationToken).ConfigureAwait(false);
                if (localDibixVersion != consumerPackageVersion)
                {
                    ConsoleUtility.WriteLineDebug($"Updating consumer package reference of package '{packageName}' from version '{consumerPackageVersion}' to '{localDibixVersion}'");
                    await ConsumerPackageManager.SetPackageVersionMSBuild(packageName, localDibixVersion, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    ConsoleUtility.WriteLineDebug($"Consumer already uses package '{packageName}' with version '{localDibixVersion}'");
                }
                if (ArtifactUtility.IsSdk(packageName))
                {
                    await ConsumerPackageManager.SetPackageVersionGlobalJson(packageName, localDibixVersion).ConfigureAwait(false);
                }
            }

            return 0;
        }
    }
}