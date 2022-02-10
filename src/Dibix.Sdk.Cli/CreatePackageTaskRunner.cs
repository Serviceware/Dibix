namespace Dibix.Sdk.Cli
{
    [TaskRunner("pack")]
    internal sealed class CreatePackageTaskRunner : InputConfigurationTaskRunner
    {
        public CreatePackageTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            CreatePackageTask.Execute
            (
                artifactName: configuration.GetSingleValue<string>("ArtifactName")
              , outputDirectory: configuration.GetSingleValue<string>("OutputDirectory")
              , compiledArtifactPath: configuration.GetSingleValue<string>("CompiledArtifactPath")
            );
        }
    }
}