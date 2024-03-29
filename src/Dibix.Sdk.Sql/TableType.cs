using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    public sealed class TableType : TableModel
    {
        private static readonly IDictionary<ModelTypeClass, ConstraintSelector> ConstraintMap = new Dictionary<ModelTypeClass, ConstraintSelector>
        {
            { ModelSchema.TableTypePrimaryKeyConstraint, new ConstraintSelector(ConstraintKind.PrimaryKey, TableTypePrimaryKeyConstraint.Columns,           TableTypePrimaryKeyConstraint.Clustered, null) }
          , { ModelSchema.TableTypeUniqueConstraint,     new ConstraintSelector(ConstraintKind.Unique,         TableTypeUniqueConstraint.Columns,               TableTypeUniqueConstraint.Clustered, null) }
          , { ModelSchema.TableTypeCheckConstraint,      new ConstraintSelector(ConstraintKind.Check,      TableTypeCheckConstraint.ExpressionDependencies, null                                   , TableTypeCheckConstraint.Expression) }
          , { ModelSchema.TableTypeDefaultConstraint,    new ConstraintSelector(ConstraintKind.Default,    TableTypeDefaultConstraint.TargetColumn,         null                                   , null) }
        };

        public override string TypeDisplayName => "User defined table type";
        protected override ModelTypeClass ObjectType => ModelSchema.TableType;
        protected override ModelRelationshipClass ColumnsRelationship => Microsoft.SqlServer.Dac.Model.TableType.Columns;
        protected override ModelTypeClass ColumnType => TableTypeColumn.TypeClass;
        protected override ModelRelationshipClass ColumnDataType => TableTypeColumn.DataType;
        protected override ModelPropertyClass ColumnLength => TableTypeColumn.Length;
        protected override ModelPropertyClass ColumnPrecision => TableTypeColumn.Precision;
        protected override ModelPropertyClass Nullable => TableTypeColumn.Nullable;

        protected override bool IsComputed(TSqlObject column) => column.GetMetadata<TableTypeColumnType>(TableTypeColumn.TableTypeColumnType) == TableTypeColumnType.ComputedColumn;
        protected override bool HasPrimaryKeyConstraint(TSqlObject table) => GetPrimaryKeyConstraint(table) != null;
        protected override IEnumerable<TSqlObject> GetPrimaryKeyColumns(TSqlObject table)
        {
            TSqlObject primaryKeyConstraint = GetPrimaryKeyConstraint(table);
            if (primaryKeyConstraint == null)
                return Enumerable.Empty<TSqlObject>();

            return primaryKeyConstraint.GetReferenced(TableTypePrimaryKeyConstraint.Columns, DacQueryScopes.All);
        }
        protected override IEnumerable<Constraint> GetConstraints(TSqlObject table)
        {
            IEnumerable<Constraint> constraints = table.GetReferenced(Microsoft.SqlServer.Dac.Model.TableType.Constraints, DacQueryScopes.All)
                                                       .Select(x => base.ToConstraint(ConstraintMap[x.ObjectType], x));
            return constraints;
        }

        private static TSqlObject GetPrimaryKeyConstraint(TSqlObject table)
        {
            return table.GetReferenced(Microsoft.SqlServer.Dac.Model.TableType.Constraints, DacQueryScopes.All)
                        .SingleOrDefault(x => x.ObjectType == TableTypePrimaryKeyConstraint.TypeClass);
        }
    }
}