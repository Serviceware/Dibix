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

            Constraint primaryKey = node.Definition
                                        .CollectConstraints()
                                        .SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (primaryKey == null)
                return;

            IDictionary<string, DataTypeReference> columns = node.Definition
                                                                 .ColumnDefinitions
                                                                 .ToDictionary(x => x.ColumnIdentifier.Value, x => x.DataType);

            UniqueConstraintDefinition constraint = (UniqueConstraintDefinition)primaryKey.Definition;
            foreach (ColumnWithSortOrder column in constraint.Columns)
            {
                string columnName = column.Column.MultiPartIdentifier.Identifiers.Last().Value;
                SqlDataTypeReference sqlDataType = columns[columnName] as SqlDataTypeReference;
                if (sqlDataType != null)
                {
                    if (!AllowedPrimaryKeyTypes.Contains(sqlDataType.SqlDataTypeOption))
                        base.Fail(column, constraint.ConstraintIdentifier.Value);
                }
            }
        }
    }
}
 