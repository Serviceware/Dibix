using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public sealed class GenerateScriptSqlStatementFormatter : SqlStatementFormatter, ISqlStatementFormatter
    {
        public override string Format(SqlStatementInfo info, StatementList body)
        {
            TSqlBatch batch = new TSqlBatch();
            batch.Statements.AddRange(base.GetStatements(body));

            Sql140ScriptGenerator generator = new Sql140ScriptGenerator { Options = { AlignClauseBodies = false } };
            generator.GenerateScript(batch, out var output);
            return output;
        }
    }
}