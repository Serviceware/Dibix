using Dibix.Sdk.CodeGeneration;

namespace Dibix.Sdk.Cli
{
    [TaskRunner("compile")]
    internal sealed class CodeGenerationTaskRunner : TaskRunner
    {
        public CodeGenerationTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            CodeGenerationTask.Execute
            (
                projectDirectory: configuration.GetSingleValue<string>("ProjectDirectory")
              , productName: configuration.GetSingleValue<string>("ProductName")
              , areaName: configuration.GetSingleValue<string>("AreaName")
              , title: configuration.GetSingleValue<string>("Title")
              , version: configuration.GetSingleValue<string>("Version")
              , description: configuration.GetSingleValue<string>("Description")
              , baseUrl: configuration.GetSingleValue<string>("BaseUrl")
              , defaultOutputFilePath: configuration.GetSingleValue<string>("DefaultOutputFilePath")
              , clientOutputFilePath: configuration.GetSingleValue<string>("ClientOutputFilePath")
              , source: configuration.GetItems("Source")
              , contracts: configuration.GetItems("Contracts")
              , endpoints: configuration.GetItems("Endpoints")
              , references: configuration.GetItems("References")
              , embedStatements: configuration.GetSingleValue<bool>("EmbedStatements")
              , databaseSchemaProviderName: configuration.GetSingleValue<string>("DatabaseSchemaProviderName")
              , modelCollation: configuration.GetSingleValue<string>("ModelCollation")
              , sqlReferencePath: configuration.GetItems("SqlReferencePath")
              , logger: base.Logger
              , additionalAssemblyReferences: out string[] _
            );
        }
    }
}