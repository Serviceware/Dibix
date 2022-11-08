using System.Data;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class GenerateScriptSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        protected override FormattedSqlStatement Format(SqlStatementDefinition statementDefinition, StatementList statementList)
        {
            TSqlBatch batch = new TSqlBatch();

            void StatementHandler(TSqlStatement statement, int statementIndex, int statementCount) => batch.Statements.Add(statement);

            base.CollectStatements(statementList, StatementHandler);

            string generated = ScriptDomFacade.Generate(batch, x => x.Options.AlignClauseBodies = false);
            return new FormattedSqlStatement(generated, CommandType.Text);
        }
    }
}