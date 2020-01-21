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
            if (node.IsTemporaryTable())
                return;

            foreach (Constraint constraint in base.Model.GetConstraints(node.SchemaObjectName))
            {
                if (constraint.Kind == ConstraintKind.Nullable || constraint.Name != null)
                    continue;

                base.Fail(constraint.Source, node.SchemaObjectName.BaseIdentifier.Value, constraint.KindDisplayName.ToLowerInvariant());
            }
        }
    }
}