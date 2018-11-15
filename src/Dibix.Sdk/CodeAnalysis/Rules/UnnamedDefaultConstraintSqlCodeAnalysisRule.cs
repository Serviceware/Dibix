using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UnnamedDefaultConstraintSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnnamedDefaultConstraintSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 14;
        public override string ErrorMessage => "Column '{0}.{1}' has an unnamed default constraint";
    }

    public sealed class UnnamedDefaultConstraintSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            node.Definition
                .ColumnDefinitions
                .Where(x => x.DefaultConstraint != null && x.DefaultConstraint.ConstraintIdentifier == null)
                .Each(x => base.Fail(x, node.SchemaObjectName.BaseIdentifier.Value, x.ColumnIdentifier.Value));
        }
    }
}