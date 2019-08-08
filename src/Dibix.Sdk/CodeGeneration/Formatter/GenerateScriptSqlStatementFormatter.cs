using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class GenerateScriptSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        public override string Format(SqlStatementInfo info, StatementList body)
        {
            TSqlBatch batch = new TSqlBatch();
            batch.Statements.AddRange(base.GetStatements(body));

            string output = ScriptDomFacade.Generate(batch, x => x.Options.AlignClauseBodies = false);
            return output;
        }
    }
}