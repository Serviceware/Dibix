using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class TemporaryTableSqlCodeAnalysisRule : SqlCodeAnalysisRule<TemporaryTableSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 21;
        public override string ErrorMessage => "The use of temporary tables is not allowed: {0}";
    }

    public sealed class TemporaryTableSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            if (node.SchemaObjectName.BaseIdentifier.Value[0] == '#')
                base.Fail(node.SchemaObjectName.BaseIdentifier, node.SchemaObjectName.BaseIdentifier.Value);
        }
    }
}