using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UnnamedConstraintSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnnamedConstraintSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 14;
        public override string ErrorMessage => "Table '{0}' has an unnamed {1}";
    }

    public sealed class UnnamedConstraintSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            node.Definition
                .CollectConstraints()
                .Where(x => x.Type != ConstraintType.Nullable && x.Definition.ConstraintIdentifier == null)
                .Each(x => base.Fail(x.Definition, node.SchemaObjectName.BaseIdentifier.Value, x.Type.ToDisplayName().ToLowerInvariant()));
        }
    }
}