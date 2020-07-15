using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using D = Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    internal sealed class Table : TableModel
    {
        private static readonly ICollection<ConstraintSelector> ConstraintMap = new Collection<ConstraintSelector>
        {
            new ConstraintSelector(ConstraintKind.PrimaryKey, PrimaryKeyConstraint.Host, PrimaryKeyConstraint.Columns,             PrimaryKeyConstraint.Clustered, null)
          , new ConstraintSelector(ConstraintKind.ForeignKey, ForeignKeyConstraint.Host, ForeignKeyConstraint.Columns,             null,                           null)
          , new ConstraintSelector(ConstraintKind.Unique,         UniqueConstraint.Host,     UniqueConstraint.Columns,             UniqueConstraint.Clustered,     null)
          , new ConstraintSelector(ConstraintKind.Check,         D.CheckConstraint.Host, D.CheckConstraint.ExpressionDependencies, null,                           D.CheckConstraint.Expression)
          , new ConstraintSelector(ConstraintKind.Default,       DefaultConstraint.Host, DefaultConstraint.TargetColumn,           null,                           null)
        };

        public override string TypeDisplayName => "Table";
        protected override ModelTypeClass ObjectType => ModelSchema.Table;
        protected override ModelRelationshipClass ColumnsRelationship => Microsoft.SqlServer.Dac.Model.Table.Columns;
        protected override ModelTypeClass ColumnType => Microsoft.SqlServer.Dac.Model.Column.TypeClass;
        protected override ModelRelationshipClass ColumnDataType => Microsoft.SqlServer.Dac.Model.Column.DataType;
        protected override ModelPropertyClass ColumnLength => Microsoft.SqlServer.Dac.Model.Column.Length;
        protected override ModelPropertyClass ColumnPrecision => Microsoft.SqlServer.Dac.Model.Column.Precision;
        protected override ModelPropertyClass Nullable => Microsoft.SqlServer.Dac.Model.Column.Nullable;

        protected override bool IsComputed(TSqlObject column) => column.GetMetadata<ColumnType>(Microsoft.SqlServer.Dac.Model.Column.ColumnType) == Microsoft.SqlServer.Dac.Model.ColumnType.ComputedColumn;
        protected override bool HasPrimaryKeyConstraint(TSqlObject table) => GetPrimaryKeyConstraint(table) != null;
        protected override IEnumerable<TSqlObject> GetPrimaryKeyColumns(TSqlObject table)
        {
            TSqlObject primaryKeyConstraint = GetPrimaryKeyConstraint(table);
            if (primaryKeyConstraint == null)
                return Enumerable.Empty<TSqlObject>();

            return primaryKeyConstraint.GetReferenced(Microsoft.SqlServer.Dac.Model.PrimaryKeyConstraint.Columns, DacQueryScopes.All);
        }
        protected override IEnumerable<Constraint> GetConstraints(TSqlObject table)
        {
            IEnumerable<Constraint> constraints = ConstraintMap.SelectMany(x => table.GetReferencing(x.Host, DacQueryScopes.All), base.ToConstraint);
            return constraints;
        }

        private static TSqlObject GetPrimaryKeyConstraint(TSqlObject table) => table.GetReferencing(PrimaryKeyConstraint.Host, DacQueryScopes.All).SingleOrDefault();
    }
}