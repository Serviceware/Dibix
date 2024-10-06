using System;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 35)]
    public sealed class AmbiguousCheckConstraintSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Ambiguous check constraints: {0}";

        public AmbiguousCheckConstraintSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            var query = Model
                            .GetTableConstraints(node.SchemaObjectName)
                            .Where(x => x.Kind == ConstraintKind.Check)
                            .Select(x => new
                            {
                                Name = x.Name, 
                                Source = x.Source,
                                Expression = x.CheckCondition.NormalizeBooleanExpression()
                            })
                            .GroupBy(x => x.Expression)
                            .Where(x => x.Count() > 1);

            foreach (var constraintGroup in query)
            {
                var orderedConstraints = constraintGroup.OrderBy(x => x.Source.StartLine)
                                                        .ThenBy(x => x.Source.StartColumn)
                                                        .ToArray();
                Fail(orderedConstraints.First().Source, String.Join(", ", orderedConstraints.Select(x => x.Name)));
            }
        }
    }
}