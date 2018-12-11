using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class SurrogateKeySqlCodeAnalysisRule : SqlCodeAnalysisRule<SurrogateKeySqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 24;
        public override string ErrorMessage => "Surrogate keys are only allowed, if a business key is defined: {0}";
    }

    public sealed class SurrogateKeySqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // helpLine suppressions
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
        };

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            ICollection<Constraint> constraints = node.Definition.CollectConstraints().ToArray();

            bool hasSurrogateKey = TryGetSurrogateKey(node, constraints, out Constraint primaryKey);
            if (!hasSurrogateKey)
                return;

            // We just assume here that a UQ could be a business key
            bool hasBusinessKey = constraints.Any(x => x.Type == ConstraintType.Unique && !((UniqueConstraintDefinition)x.Definition).IsPrimaryKey);
            if (hasBusinessKey)
                return;

            string identifier = $"({primaryKey.Columns.Single().Name})";
            if (primaryKey.Definition.ConstraintIdentifier != null)
                identifier = String.Concat(primaryKey.Definition.ConstraintIdentifier.Value, identifier);
            else
                identifier = String.Concat(node.SchemaObjectName.BaseIdentifier.Value, identifier);

            if (Workarounds.Contains(identifier))
                return;

            base.Fail(node, node.SchemaObjectName.BaseIdentifier.Value);
        }

        private static bool TryGetSurrogateKey(CreateTableStatement createTableStatement, IEnumerable<Constraint> constraints, out Constraint primaryKey)
        {
            // PK
            primaryKey = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return false;

            // We only support PK with one column
            if (primaryKey.Columns.Count > 1)
                return false;

            // IDENTITY
            string primaryKeyColumnName = primaryKey.Columns.Single().Name;
            ColumnDefinition primaryKeyColumn = createTableStatement.Definition.ColumnDefinitions.Single(x => x.ColumnIdentifier.Value == primaryKeyColumnName);
            if (primaryKeyColumn.IdentityOptions == null)
                return false;

            return true;
        }
    }
}
 