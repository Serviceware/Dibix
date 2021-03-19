namespace Dibix.Sdk.Cli
{
    [TaskRunner("core")]
    internal sealed class SqlCoreTaskRunner : TaskRunner
    {
        public SqlCoreTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            SqlCoreTask.Execute
            (
                projectName: configuration.GetSingleValue<string>("ProjectName")
              , projectDirectory: configuration.GetSingleValue<string>("ProjectDirectory")
              , namingConventionPrefix: configuration.GetSingleValue<string>("NamingConventionPrefix")
              , staticCodeAnalysisSucceededFile: configuration.GetSingleValue<string>("StaticCodeAnalysisSucceededFile")
              , resultsFile: configuration.GetSingleValue<string>("ResultsFile")
              , productName: configuration.GetSingleValue<string>("ProductName")
              , areaName: configuration.GetSingleValue<string>("AreaName")
              , title: configuration.GetSingleValue<string>("Title")
              , version: configuration.GetSingleValue<string>("Version")
              , description: configuration.GetSingleValue<string>("Description")
              , baseUrl: configuration.GetSingleValue<string>("BaseUrl")
              , defaultOutputFilePath: configuration.GetSingleValue<string>("DefaultOutputFilePath")
              , clientOutputFilePath: configuration.GetSingleValue<string>("ClientOutputFilePath")
              , externalAssemblyReferenceDirectory: configuration.GetSingleValue<string>("ExternalAssemblyReferenceDir")
              , source: configuration.GetItems("Source")
              , scriptSource: configuration.GetItems("ScriptSource")
              , contracts: configuration.GetItems("Contracts")
              , endpoints: configuration.GetItems("Endpoints")
              , references: configuration.GetItems("References")
              , securitySchemes: configuration.GetItems("SecuritySchemes")
              , isEmbedded: configuration.GetSingleValue<bool>("IsEmbedded")
              , databaseSchemaProviderName: configuration.GetSingleValue<string>("DatabaseSchemaProviderName")
              , modelCollation: configuration.GetSingleValue<string>("ModelCollation")
              , sqlReferencePath: configuration.GetItems("SqlReferencePath")
              , logger: base.Logger
              , additionalAssemblyReferences: out string[] _
            );
        }
    }
}