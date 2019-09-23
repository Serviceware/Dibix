using System;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class AmbiguousCheckConstraintSqlCodeAnalysisRule : SqlCodeAnalysisRule<AmbiguousCheckConstraintSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 35;
        public override string ErrorMessage => "Ambiguous check constraints: {0}";
    }

    public sealed class AmbiguousCheckConstraintSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            var query = base.Model
                            .GetConstraints(node.SchemaObjectName)
                            .Where(x => x.Kind == ConstraintKind.Check)
                            .Select(x => new { Definition = x, Expression = ((CheckConstraintDefinition)x.Definition).CheckCondition.Normalize() })
                            .GroupBy(x => x.Expression)
                            .Where(x => x.Count() > 1);

            foreach (var constraintGroup in query)
            {
                base.Fail(constraintGroup.First().Definition.Source, String.Join(", ", constraintGroup.Select(x => x.Definition.Name)));
            }
        }
    }
}