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
        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            ICollection<Constraint> constraints = node.Definition
                                                      .CollectConstraints()
                                                      .ToArray();

            bool hasSurrogateKey = HasSurrogateKey(node, constraints);

            // We just assume here that a UQ could be a business key
            bool hasBusinessKey = constraints.Any(x => x.Type == ConstraintType.Unique && !((UniqueConstraintDefinition)x.Definition).IsPrimaryKey);

            if (hasSurrogateKey && !hasBusinessKey)
                base.Fail(node, node.SchemaObjectName.BaseIdentifier.Value);
        }

        private static bool HasSurrogateKey(CreateTableStatement createTableStatement, IEnumerable<Constraint> constraints)
        {
            // PK
            Constraint primaryKey = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return false;

            // We only support PK with one column
            UniqueConstraintDefinition primaryKeyConstraint = (UniqueConstraintDefinition)primaryKey.Definition;
            if (primaryKeyConstraint.Columns.Count > 1)
                return false;

            // IDENTITY
            string primaryKeyColumnName = primaryKeyConstraint.Columns.Any() ? primaryKeyConstraint.Columns[0].Column.MultiPartIdentifier.Identifiers.Last().Value : primaryKey.ParentName;
            ColumnDefinition primaryKeyColumn = createTableStatement.Definition.ColumnDefinitions.Single(x => x.ColumnIdentifier.Value == primaryKeyColumnName);
            if (primaryKeyColumn.IdentityOptions == null)
                return false;

            return true;
        }
    }
}
 