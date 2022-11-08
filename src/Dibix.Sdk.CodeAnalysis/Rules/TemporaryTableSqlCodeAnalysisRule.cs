using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 21)]
    public sealed class TemporaryTableSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "The use of temporary tables is not allowed: {0}";

        public TemporaryTableSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                base.Fail(node.SchemaObjectName.BaseIdentifier, node.SchemaObjectName.BaseIdentifier.Value);
        }
    }
}