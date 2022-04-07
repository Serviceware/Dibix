namespace Dibix.Sdk.Cli
{
    [TaskRunner("core")]
    internal sealed class SqlCoreTaskRunner : InputConfigurationTaskRunner
    {
        public SqlCoreTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            SqlCoreTask.Execute
            (
                projectName: configuration.GetSingleValue<string>("ProjectName")
              , projectDirectory: configuration.GetSingleValue<string>("ProjectDirectory")
              , configurationFilePath: configuration.GetSingleValue<string>("ConfigurationFilePath")
              , staticCodeAnalysisSucceededFile: configuration.GetSingleValue<string>("StaticCodeAnalysisSucceededFile")
              , resultsFile: configuration.GetSingleValue<string>("ResultsFile")
              , productName: configuration.GetSingleValue<string>("ProductName")
              , areaName: configuration.GetSingleValue<string>("AreaName")
              , title: configuration.GetSingleValue<string>("Title")
              , version: configuration.GetSingleValue<string>("Version")
              , description: configuration.GetSingleValue<string>("Description")
              , outputDirectory: configuration.GetSingleValue<string>("OutputDirectory")
              , defaultOutputName: configuration.GetSingleValue<string>("DefaultOutputName")
              , clientOutputName: configuration.GetSingleValue<string>("ClientOutputName")
              , externalAssemblyReferenceDirectory: configuration.GetSingleValue<string>("ExternalAssemblyReferenceDir")
              , source: configuration.GetItems("Source")
              , scriptSource: configuration.GetItems("ScriptSource")
              , contracts: configuration.GetItems("Contracts")
              , endpoints: configuration.GetItems("Endpoints")
              , references: configuration.GetItems("References")
              , defaultSecuritySchemes: configuration.GetItems("DefaultSecuritySchemes")
              , isEmbedded: configuration.GetSingleValue<bool>("IsEmbedded")
              , enableExperimentalFeatures: configuration.GetSingleValue<bool>("EnableExperimentalFeatures")
              , databaseSchemaProviderName: configuration.GetSingleValue<string>("DatabaseSchemaProviderName")
              , modelCollation: configuration.GetSingleValue<string>("ModelCollation")
              , sqlReferencePath: configuration.GetItems("SqlReferencePath")
              , logger: base.Logger
                // This property is ignored, since we currently don't have a way to output properties from the CLI to MSbuild
                // Therefore all additional assembly references are registered statically within the target
              , additionalAssemblyReferences: out string[] _
            );
        }
    }
}