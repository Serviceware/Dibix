using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    internal sealed class Table : TableModel
    {
        private static readonly ICollection<ConstraintSelector> ConstraintMap = new Collection<ConstraintSelector>
        {
            new ConstraintSelector(ConstraintKind.PrimaryKey, PrimaryKeyConstraint.Host, PrimaryKeyConstraint.Columns,   PrimaryKeyConstraint.Clustered)
          , new ConstraintSelector(ConstraintKind.ForeignKey, ForeignKeyConstraint.Host, ForeignKeyConstraint.Columns,   null)
          , new ConstraintSelector(ConstraintKind.Unique,         UniqueConstraint.Host,     UniqueConstraint.Columns,       UniqueConstraint.Clustered)
          , new ConstraintSelector(ConstraintKind.Check,           CheckConstraint.Host, null,                           null)
          , new ConstraintSelector(ConstraintKind.Default,       DefaultConstraint.Host, DefaultConstraint.TargetColumn, null)
        };

        public override string TypeDisplayName => "Table";
        public override ModelTypeClass ObjectType => ModelSchema.Table;
        public override ModelRelationshipClass ColumnDataType => Microsoft.SqlServer.Dac.Model.Column.DataType;
        public override ModelPropertyClass ColumnLength => Microsoft.SqlServer.Dac.Model.Column.Length;
        public override ModelPropertyClass ColumnPrecision => Microsoft.SqlServer.Dac.Model.Column.Precision;

        protected override bool IsComputed(TSqlObject column) => column.GetMetadata<ColumnType>(Microsoft.SqlServer.Dac.Model.Column.ColumnType) == ColumnType.ComputedColumn;
        protected override bool HasPrimaryKey(TSqlObject table) => table.GetReferencing(PrimaryKeyConstraint.Host).Any();

        protected override IEnumerable<Constraint> GetConstraints(TSqlObject table)
        {
            IEnumerable<Constraint> constraints = ConstraintMap.SelectMany(x => table.GetReferencing(x.Host), base.ToConstraint);
            return constraints;
        }
    }
}