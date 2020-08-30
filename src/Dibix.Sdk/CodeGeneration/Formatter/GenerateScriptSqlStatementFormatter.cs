using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class GenerateScriptSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        protected override string Format(SqlStatementInfo info, StatementList statementList)
        {
            TSqlBatch batch = new TSqlBatch();

            void StatementHandler(TSqlStatement statement, int statementIndex, int statementCount) => batch.Statements.Add(statement);

            base.CollectStatements(statementList, StatementHandler);

            string generated = ScriptDomFacade.Generate(batch, x => x.Options.AlignClauseBodies = false);
            return generated;
        }
    }
}