using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule<PrimaryKeyDataTypeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 23;
        public override string ErrorMessage => "Only TINYINT/SMALLINT/INT/BIGINT are allowed as primary key: {0}";
    }

    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private static readonly HashSet<SqlDataTypeOption> AllowedPrimaryKeyTypes = new HashSet<SqlDataTypeOption>
        {
            SqlDataTypeOption.TinyInt
          , SqlDataTypeOption.SmallInt
          , SqlDataTypeOption.Int
          , SqlDataTypeOption.BigInt
        };

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            ICollection<Constraint> constraints = node.Definition
                                                      .CollectConstraints()
                                                      .ToArray();

            Constraint primaryKey = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return;

            IDictionary<string, DataTypeReference> columns = node.Definition
                                                                 .ColumnDefinitions
                                                                 .ToDictionary(x => x.ColumnIdentifier.Value, x => x.DataType);

            UniqueConstraintDefinition primaryKeyConstraint = (UniqueConstraintDefinition)primaryKey.Definition;

            // If the PK is not the table's own key, and instead is a FK to a different table's key, no further analysis is needed
            bool hasMatchingForeignKey = constraints.Where(x => x.Type == ConstraintType.ForeignKey).Any(x => AreEqual(primaryKeyConstraint, (ForeignKeyConstraintDefinition)x.Definition));
            if (hasMatchingForeignKey)
                return;

            foreach (ColumnWithSortOrder column in primaryKeyConstraint.Columns)
            {
                string columnName = column.Column.MultiPartIdentifier.Identifiers.Last().Value;
                if (columns[columnName] is SqlDataTypeReference sqlDataType && !AllowedPrimaryKeyTypes.Contains(sqlDataType.SqlDataTypeOption))
                    base.Fail(column, primaryKeyConstraint.ConstraintIdentifier.Value);
            }
        }

        private static bool AreEqual(UniqueConstraintDefinition uniqueConstraint, ForeignKeyConstraintDefinition foreignKeyConstraint)
        {
            if (uniqueConstraint.Columns.Count != foreignKeyConstraint.Columns.Count)
                return false;

            for (int i = 0; i < uniqueConstraint.Columns.Count; i++)
            {
                string uniqueColumnName = uniqueConstraint.Columns[i].Column.MultiPartIdentifier.Identifiers.Last().Value;
                string foreignKeyColumnName = foreignKeyConstraint.Columns[i].Value;
                if (uniqueColumnName != foreignKeyColumnName)
                    return false;
            }

            return true;
        }
    }
}
 