using Dibix.Sdk.CodeAnalysis;

namespace Dibix.Sdk.Cli
{
    [TaskRunner("sqlca")]
    internal sealed class SqlCodeAnalysisTaskRunner : TaskRunner
    {
        public SqlCodeAnalysisTaskRunner(ILogger logger) : base(logger) { }

        protected override void Execute(InputConfiguration configuration)
        {
            SqlCodeAnalysisTask.Execute
            (
                namingConventionPrefix: configuration.GetSingleValue<string>("NamingConventionPrefix")
              , databaseSchemaProviderName: configuration.GetSingleValue<string>("DatabaseSchemaProviderName")
              , modelCollation: configuration.GetSingleValue<string>("ModelCollation")
              , source: configuration.GetItems("Source")
              , scriptSource: configuration.GetItems("ScriptSource")
              , sqlReferencePath: configuration.GetItems("SqlReferencePath")
              , logger: base.Logger
            );
        }
    }
}