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
              , outputName: configuration.GetSingleValue<string>("OutputName")
              , title: configuration.GetSingleValue<string>("Title")
              , version: configuration.GetSingleValue<string>("Version")
              , description: configuration.GetSingleValue<string>("Description")
              , baseUrl: configuration.GetSingleValue<string>("BaseUrl")
              , defaultOutputFilePath: configuration.GetSingleValue<string>("DefaultOutputFilePath")
              , endpointOutputFilePath: configuration.GetSingleValue<string>("EndpointOutputFilePath")
              , clientOutputFilePath: configuration.GetSingleValue<string>("ClientOutputFilePath")
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