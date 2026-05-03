using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Sdk.Cli
{
    internal sealed class AbandonArtifactCommand : ValidatableActionCommand
    {
        private readonly Argument<string> _artifactNameArgument;
        private readonly EnvironmentVariableOption _nuGetApiKeyOption;
        private readonly EnvironmentVariableOption _dockerHubUserNameOption;
        private readonly EnvironmentVariableOption _dockerHubPasswordOption;
        private readonly Option<string> _dockerOrganizationOption;
        private string _nuGetApiKey;
        private string _dockerHubUserName;
        private string _dockerHubPassword;
        private (string ArtifactName, bool IsApplication)? _artifactToAbandon;

        public AbandonArtifactCommand() : base("abandon", "Unlists NuGet package(s) from the official feed and removes the Docker image from the hub.")
        {
            _artifactNameArgument = new Argument<string>("artifact-name")
            {
                Description = "The name of the artifact to abandon. If not specified, all packages will be abandoned.",
                Arity = ArgumentArity.ZeroOrOne
            };
            _nuGetApiKeyOption = new EnvironmentVariableOption("--nuget-api-key", EnvironmentVariableName.NuGetPackageOfficialFeedApiKey, "The API key used to unlist the package(s) from the official feed.", "-n");
            _dockerHubUserNameOption = new EnvironmentVariableOption("--docker-hub-user", EnvironmentVariableName.DockerHubUserName, "The user name used to delete the docker image(s) from the Docker Hub.", "-u");
            _dockerHubPasswordOption = new EnvironmentVariableOption("--docker-hub-password", EnvironmentVariableName.DockerHubPassword, "The password used to delete the docker image(s) from the Docker Hub.", "-p");
            _dockerOrganizationOption = new Option<string>("--docker-organization", "-o")
            {
                Description = "The organization name of the Docker Hub repository. Default: servicewareit",
                DefaultValueFactory = _ => "servicewareit"
            };

            Add(_artifactNameArgument);
            Add(_nuGetApiKeyOption);
            Add(_dockerHubUserNameOption);
            Add(_dockerHubPasswordOption);
            Add(_dockerOrganizationOption);
        }

        protected override void Validate(CommandResult commandResult)
        {
            bool loggedMessages = false;
            _nuGetApiKey = _nuGetApiKeyOption.CollectValue(commandResult, isRequired: true, ref loggedMessages);
            _dockerHubUserName = _dockerHubUserNameOption.CollectValue(commandResult, isRequired: true, ref loggedMessages);
            _dockerHubPassword = _dockerHubPasswordOption.CollectValue(commandResult, isRequired: true, ref loggedMessages);

            string artifactToAbandon = commandResult.GetValue(_artifactNameArgument);

            if (artifactToAbandon != null)
            {
                if (!ArtifactUtility.AllArtifacts.TryGetValue(artifactToAbandon, out bool isApplication))
                {
                    commandResult.AddError($"""
                                            Unknown artifact '{artifactToAbandon}'.
                                            Possible values are: {String.Join(", ", ArtifactUtility.AllArtifacts.Keys)}
                                            """);
                }
                _artifactToAbandon = (ArtifactName: artifactToAbandon, IsApplication: isApplication);
            }

            if (loggedMessages)
                Console.WriteLine();
        }

        protected override async Task<int> Execute(ParseResult parseResult, CancellationToken cancellationToken)
        {
            if (_nuGetApiKey == null)
                throw new InvalidOperationException("API key not initialized.");

            string artifactToAbandon = parseResult.GetValue(_artifactNameArgument);
            string dockerHubOrganization = parseResult.GetValue(_dockerOrganizationOption);

            ConsoleUtility.WriteLineInformation(artifactToAbandon == null ? "Abandoning all artifacts" : $"Abandoning only artifact '{artifactToAbandon}'");

            (string ArtifactName, bool IsApplication)[] artifactsToDeploy = _artifactToAbandon != null ? [_artifactToAbandon.Value] : [..ArtifactUtility.AllArtifacts.Select(x => (x.Key, x.Value))];
            string localDibixNuGetPackageVersion = await ArtifactUtility.GetLocalDibixNuGetPackageVersion(cancellationToken).ConfigureAwait(false);
            string localDibixImageTag = await ArtifactUtility.GetLocalDibixDockerImageTag(cancellationToken).ConfigureAwait(false);
            Lazy<Task<string>> dockerHubAccessTokenAccessor = new Lazy<Task<string>>(GetDockerHubAccessToken);

            ConsoleUtility.WriteLineDebug($"Local Dibix version: {localDibixNuGetPackageVersion}");

            foreach ((string artifactName, bool isApplication) in artifactsToDeploy)
            {
                ConsoleUtility.WriteLineInformation(artifactName);

                if (isApplication)
                {
                    string accessToken = await dockerHubAccessTokenAccessor.Value.ConfigureAwait(false);
                    ConsoleUtility.WriteLineDebug($"Removing Docker image '{dockerHubOrganization}/{artifactName}:{localDibixImageTag}' from Docker Hub");
                    await ArtifactUtility.RemoveDockerImageFromDockerHub(dockerHubOrganization, repository: artifactName, tag: localDibixImageTag, accessToken, cancellationToken).ConfigureAwait(false);

                    ConsoleUtility.WriteLineDebug($"Removing Docker image '{dockerHubOrganization}/{artifactName}:{localDibixImageTag}' from local cache");
                    await ArtifactUtility.RemoveDockerImageFromLocalCache(dockerHubOrganization, repository: artifactName, tag: localDibixImageTag, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    ConsoleUtility.WriteLineDebug($"Unlisting package '{artifactName}' version '{localDibixNuGetPackageVersion}' from official feed");
                    await ArtifactUtility.UnlistNuGetPackage(artifactName, localDibixNuGetPackageVersion, _nuGetApiKey, cancellationToken).ConfigureAwait(false);

                    ConsoleUtility.WriteLineDebug($"Removing package '{artifactName}' version '{localDibixNuGetPackageVersion}' from local NuGet package cache");
                    ArtifactUtility.RemovePackageFromNuGetPackageCache(artifactName, localDibixNuGetPackageVersion);
                }
            }

            return 0;
        }

        private async Task<string> GetDockerHubAccessToken()
        {
            ConsoleUtility.WriteLineDebug("Authenticating with Docker Hub");
            string accessToken = await ArtifactUtility.GetDockerHubAccessToken(_dockerHubUserName, _dockerHubPassword).ConfigureAwait(false);
            return accessToken;
        }
    }
}