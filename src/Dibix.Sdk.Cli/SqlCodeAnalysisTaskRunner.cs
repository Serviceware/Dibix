using Dibix.Sdk.CodeAnalysis;

namespace Dibix.Sdk.Cli
{
    [TaskRunner("analyze")]
    internal sealed class SqlCodeAnalysisTaskRunner : InputConfigurationTaskRunner
    {
        public SqlCodeAnalysisTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            SqlCodeAnalysisTask.Execute
            (
                projectName: configuration.GetSingleValue<string>("ProjectName")
              , databaseSchemaProviderName: configuration.GetSingleValue<string>("DatabaseSchemaProviderName")
              , modelCollation: configuration.GetSingleValue<string>("ModelCollation")
              , namingConventionPrefix: configuration.GetSingleValue<string>("NamingConventionPrefix")
              , isEmbedded: configuration.GetSingleValue<bool>("IsEmbedded")
              , staticCodeAnalysisSucceededFile: configuration.GetSingleValue<string>("StaticCodeAnalysisSucceededFile")
              , resultsFile: configuration.GetSingleValue<string>("ResultsFile")
              , source: configuration.GetItems("Source")
              , scriptSource: configuration.GetItems("ScriptSource")
              , sqlReferencePath: configuration.GetItems("SqlReferencePath")
              , logger: base.Logger
            );
        }
    }
}