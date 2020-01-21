using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    internal sealed class TableType : TableModel
    {
        private static readonly IDictionary<ModelTypeClass, ConstraintSelector> ConstraintMap = new Dictionary<ModelTypeClass, ConstraintSelector>
        {
            { ModelSchema.TableTypePrimaryKeyConstraint, new ConstraintSelector(ConstraintKind.PrimaryKey, TableTypePrimaryKeyConstraint.Columns,           TableTypePrimaryKeyConstraint.Clustered, null) }
          , { ModelSchema.TableTypeUniqueConstraint,     new ConstraintSelector(ConstraintKind.Unique,         TableTypeUniqueConstraint.Columns,               TableTypeUniqueConstraint.Clustered, null) }
          , { ModelSchema.TableTypeCheckConstraint,      new ConstraintSelector(ConstraintKind.Check,      TableTypeCheckConstraint.ExpressionDependencies, null                                   , TableTypeCheckConstraint.Expression) }
          , { ModelSchema.TableTypeDefaultConstraint,    new ConstraintSelector(ConstraintKind.Default,    TableTypeDefaultConstraint.TargetColumn,         null                                   , null) }
        };

        public override string TypeDisplayName => "User defined table type";
        public override ModelTypeClass ObjectType => ModelSchema.TableType;
        public override ModelTypeClass ColumnType => TableTypeColumn.TypeClass;
        public override ModelRelationshipClass ColumnDataType => TableTypeColumn.DataType;
        public override ModelPropertyClass ColumnLength => TableTypeColumn.Length;
        public override ModelPropertyClass ColumnPrecision => TableTypeColumn.Precision;
        public override ModelPropertyClass Nullable => TableTypeColumn.Nullable;

        protected override bool IsComputed(TSqlObject column) => column.GetMetadata<TableTypeColumnType>(TableTypeColumn.TableTypeColumnType) == TableTypeColumnType.ComputedColumn;
        protected override bool HasPrimaryKey(TSqlObject table) => table.GetReferenced(Microsoft.SqlServer.Dac.Model.TableType.Constraints).Any(x => x.ObjectType == TableTypePrimaryKeyConstraint.TypeClass);

        protected override IEnumerable<Constraint> GetConstraints(TSqlObject table)
        {
            IEnumerable<Constraint> constraints = table.GetReferenced(Microsoft.SqlServer.Dac.Model.TableType.Constraints)
                                                       .Select(x => base.ToConstraint(ConstraintMap[x.ObjectType], x));
            return constraints;
        }
    }
}