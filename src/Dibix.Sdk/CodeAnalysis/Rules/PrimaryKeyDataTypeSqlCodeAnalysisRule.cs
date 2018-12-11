using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule<PrimaryKeyDataTypeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 23;
        public override string ErrorMessage => "Only TINYINT/SMALLINT/INT/BIGINT are allowed as primary key: {0}.{1} ({2})";
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
        // helpLine suppressions
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
        };

        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            ICollection<Constraint> constraints = node.Definition.CollectConstraints().ToArray();

            Constraint primaryKey = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return;

            IDictionary<string, DataTypeReference> columns = node.Definition
                                                                 .ColumnDefinitions
                                                                 .ToDictionary(x => x.ColumnIdentifier.Value, x => x.DataType);

            UniqueConstraintDefinition primaryKeyConstraint = (UniqueConstraintDefinition)primaryKey.Definition;

            // If the PK is not the table's own key, and instead is a FK to a different table's key, no further analysis is needed
            bool hasMatchingForeignKey = constraints.Where(x => x.Type == ConstraintType.ForeignKey).Any(x => AreEqual(primaryKey, x));
            if (hasMatchingForeignKey)
                return;

            string tableName = node.SchemaObjectName.BaseIdentifier.Value;
            foreach (ColumnReference column in primaryKey.Columns)
            {
                string identifier = column.Name;
                if (primaryKeyConstraint.ConstraintIdentifier != null)
                    identifier = String.Concat(primaryKeyConstraint.ConstraintIdentifier.Value, '#', identifier);
                else
                    identifier = String.Concat(tableName, '#', identifier);

                if (columns[column.Name] is SqlDataTypeReference sqlDataType
                 && !AllowedPrimaryKeyTypes.Contains(sqlDataType.SqlDataTypeOption)
                 && !Workarounds.Contains(identifier))
                {
                    base.Fail(column.Hit, tableName, column.Name, sqlDataType.SqlDataTypeOption.ToString().ToUpperInvariant());
                }
            }
        }

        private static bool AreEqual(Constraint uniqueConstraint, Constraint foreignKeyConstraint)
        {
            if (uniqueConstraint.Columns.Count != foreignKeyConstraint.Columns.Count)
                return false;

            for (int i = 0; i < uniqueConstraint.Columns.Count; i++)
            {
                string uniqueColumnName = uniqueConstraint.Columns[i].Name;
                string foreignKeyColumnName = foreignKeyConstraint.Columns[i].Name;
                if (uniqueColumnName != foreignKeyColumnName)
                    return false;
            }

            return true;
        }
    }
}