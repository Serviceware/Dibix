using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 14)]
    public sealed class UnnamedConstraintSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Table '{0}' has an unnamed {1}";

        public UnnamedConstraintSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            foreach (Constraint constraint in base.Model.GetTableConstraints(node.SchemaObjectName))
            {
                if (constraint.Kind == ConstraintKind.Nullable || constraint.Name != null)
                    continue;

                base.Fail(constraint.Source, node.SchemaObjectName.BaseIdentifier.Value, constraint.KindDisplayName.ToLowerInvariant());
            }
        }
    }
}