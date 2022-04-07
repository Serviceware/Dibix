using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.Cli
{
    [TaskRunner("compile")]
    internal sealed class CodeGenerationTaskRunner : InputConfigurationTaskRunner
    {
        public CodeGenerationTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            CodeGenerationTask.Execute
            (
                projectName: configuration.GetSingleValue<string>("ProjectName")
              , projectDirectory: configuration.GetSingleValue<string>("ProjectDirectory")
              , productName: configuration.GetSingleValue<string>("ProductName")
              , areaName: configuration.GetSingleValue<string>("AreaName")
              , title: configuration.GetSingleValue<string>("Title")
              , version: configuration.GetSingleValue<string>("Version")
              , description: configuration.GetSingleValue<string>("Description")
              , baseUrl: configuration.GetSingleValue<string>("BaseUrl")
              , outputDirectory: configuration.GetSingleValue<string>("OutputDirectory")
              , defaultOutputName: configuration.GetSingleValue<string>("DefaultOutputName")
              , clientOutputName: configuration.GetSingleValue<string>("ClientOutputName")
              , externalAssemblyReferenceDirectory: configuration.GetSingleValue<string>("ExternalAssemblyReferenceDir")
              , source: configuration.GetItems("Source")
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
              , additionalAssemblyReferences: out string[] _
            );
        }
    }
}