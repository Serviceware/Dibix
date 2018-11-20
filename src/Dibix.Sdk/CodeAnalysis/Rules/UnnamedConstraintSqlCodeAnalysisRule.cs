using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UnnamedConstraintSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnnamedConstraintSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 14;
        public override string ErrorMessage => "Column '{0}.{1}' has an unnamed {2}";
    }

    public sealed class UnnamedConstraintSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            node.Definition
                .CollectConstraints(ConstraintScope.Column)
                .Where(x => x.Type != ConstraintType.Nullable && x.Definition.ConstraintIdentifier == null)
                .Each(x => base.Fail(x.Definition, node.SchemaObjectName.BaseIdentifier.Value, x.ParentName, x.Type.ToDisplayName().ToLowerInvariant()));
        }
    }
}