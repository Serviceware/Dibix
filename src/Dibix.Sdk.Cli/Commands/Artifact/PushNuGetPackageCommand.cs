using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal sealed class PushNuGetPackageCommand : ValidatableActionCommand
    {
        private readonly Argument<string> _packageNameArgument;
        private readonly Option<bool> _buildPackageOption;
        private readonly Option<string> _configurationOption;
        private readonly EnvironmentVariableOption _packageSourceOption;
        private readonly EnvironmentVariableOption _apiKeyOption;
        private string _packageFeed;
        private string _apiKey;

        public PushNuGetPackageCommand() : base("push", "Pushes NuGet package(s) to a unofficial feed. This is useful for providing preview versions for development on a private feed.")
        {
            _packageNameArgument = new Argument<string>("package-name")
            {
                Description = "The name of the package to deploy. If not specified, all packages will be deployed.",
                Arity = ArgumentArity.ZeroOrOne
            };
            _buildPackageOption = new Option<bool>("--build", "-b")
            {
                Description = "Choose to build the NuGet packages before pushing them. Default: true.",
                DefaultValueFactory = _ => true
            };
            _configurationOption = new Option<string>("--configuration", "-c")
            {
                Description = "The build configuration to use when creating the NuGet package(s).",
                DefaultValueFactory = _ => "Release"
            };
            _packageSourceOption = new EnvironmentVariableOption("--source", EnvironmentVariableName.NuGetPackageFeedSource, "The NuGet package source to push to. URL, UNC/folder path or package source name.", "-s");
            _apiKeyOption = new EnvironmentVariableOption("--api-key", EnvironmentVariableName.NuGetPackageFeedApiKey, "The API key to use when pushing to the NuGet package feed.", "-k");

            Add(_packageNameArgument);
            Add(_buildPackageOption);
            Add(_configurationOption);
            Add(_packageSourceOption);
            Add(_apiKeyOption);
        }

        protected override void Validate(CommandResult commandResult)
        {
            bool loggedMessages = false;
            _packageFeed = _packageSourceOption.CollectValue(commandResult, isRequired: true, ref loggedMessages);

            /*
              dotnet nuget push wants us to provide an --api-key parameter, but we want to pass it as an environment variable for security reasons.
              Passing a dummy value for --api-key works even without the PAT environment variable, possibly, because we are already authenticated to the Azure DevOps OnPremise Server another way.
              Continue implementing this, once it can actually be tested properly.

              warn : No API Key was provided and no API Key could be found for 'https://azdops.serviceware.net/sw/Platform/_packaging/3224b6b3-d772-4ff3-a36f-186f888fa510/nuget/v2/'. To save an API Key for a source use the 'setApiKey' command.
              Pushing Dibix.1.7.75-g925822bad6.nupkg to 'https://azdops.serviceware.net/sw/Platform/_packaging/3224b6b3-d772-4ff3-a36f-186f888fa510/nuget/v2/'...
                PUT https://azdops.serviceware.net/sw/Platform/_packaging/3224b6b3-d772-4ff3-a36f-186f888fa510/nuget/v2/
                BadRequest https://azdops.serviceware.net/sw/Platform/_packaging/3224b6b3-d772-4ff3-a36f-186f888fa510/nuget/v2/ 323ms
              error: Response status code does not indicate success: 400 (Bad Request - The request to the server did not include the header X-NuGet-ApiKey, but it is required even though credentials were provided. If using NuGet.exe, use the -ApiKey option to set this to an arbitrary value, for example "-ApiKey AzureDevOps" (DevOps Activity ID: 320E798F-9FBE-4DB9-9CF6-3D5DA69B8DAF))
            */
            //_apiKey = _apiKeyOption.CollectValue(commandResult, isRequired: true, ref loggedMessages);
            _apiKey = null;

            if (loggedMessages)
                Console.WriteLine();
        }

        protected override async Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            if (_packageFeed == null)
                throw new InvalidOperationException("Package feed not initialized.");

            //if (_apiKey == null)
            //    throw new InvalidOperationException("API key not initialized.");

            string packageToPush = parseResult.GetValue(_packageNameArgument);
            ConsoleUtility.WriteLineInformation(packageToPush == null ? "Pushing all packages" : $"Pushing only package '{packageToPush}'");

            string[] packagesToDeploy = packageToPush != null ? [packageToPush] : PackageUtility.NuGetPackageNames;
            string localDibixVersion = await PackageUtility.GetLocalDibixVersion().ConfigureAwait(false);
            string configuration = parseResult.GetRequiredValue(_configurationOption);
            bool build = parseResult.GetRequiredValue(_buildPackageOption);

            ConsoleUtility.WriteLineDebug($"Local Dibix version: {localDibixVersion}");

            if (build)
            {
                ConsoleUtility.WriteLineDebug("Restoring NuGet packages");
                await PackageUtility.RestoreNuGetPackages().ConfigureAwait(false);
            }

            foreach (string packageName in packagesToDeploy)
            {
                ConsoleUtility.WriteLineInformation(packageName);

                if (build)
                {
                    ConsoleUtility.WriteLineDebug($"Creating NuGet package for '{packageName}'");
                    await PackageUtility.CreateNuGetPackage(packageName, localDibixVersion, configuration).ConfigureAwait(false);
                }

                ConsoleUtility.WriteLineDebug($"Pushing package '{packageName}' version '{localDibixVersion}' to feed '{_packageFeed}'");
                await PackageUtility.PushNuGetPackage(packageName, localDibixVersion, configuration, _packageFeed, _apiKey).ConfigureAwait(false);
            }

            return 0;
        }
    }
}