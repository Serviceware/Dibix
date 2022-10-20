using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Column = Dibix.Sdk.Sql.Column;
using TableType = Dibix.Sdk.Sql.TableType;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 23)]
    public sealed class PrimaryKeyDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private readonly IDictionary<string, TSqlFragment> _primaryKeyColumnLocations;

        protected override string ErrorMessageTemplate => "Only TINYINT/SMALLINT/INT/BIGINT are allowed as primary key: {0}.{1} ({2})";

        public PrimaryKeyDataTypeSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context)
        {
            this._primaryKeyColumnLocations = new Dictionary<string, TSqlFragment>();
        }

        protected override void BeginStatement(TSqlScript node)
        {
            PrimaryKeyColumnLocationVisitor visitor = new PrimaryKeyColumnLocationVisitor();
            node.Accept(visitor);
            this._primaryKeyColumnLocations.ReplaceWith(visitor.PrimaryKeyColumnLocations);
        }

        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            // TODO: Clarify if this rule should also be applied for UDTs
            if (tableModel is TableType)
                return;

            ICollection<Constraint> constraints = base.Model.GetConstraints(tableModel, tableName).ToArray();

            Constraint primaryKey = constraints.SingleOrDefault(x => x.Kind == ConstraintKind.PrimaryKey);
            if (primaryKey == null)
                return;

            string actualTableName = tableName.BaseIdentifier.Value;
            foreach (Column column in primaryKey.Columns)
            {
                if (column.IsComputed)
                    continue;

                // If the PK is not the table's own key, and instead is a FK to a different table's key, no further analysis is needed
                bool hasMatchingForeignKey = constraints.Where(x => x.Kind == ConstraintKind.ForeignKey)
                                                        .Any(x => x.Columns.Any(y => y.Name == column.Name));
                if (hasMatchingForeignKey)
                    continue;

                string identifier = column.Name;
                if (primaryKey.Name != null)
                    identifier = String.Concat(primaryKey.Name, '#', identifier);
                else
                    identifier = String.Concat(actualTableName, '#', identifier);

                if (PrimaryKeyDataType.AllowedTypes.Contains(column.SqlDataType))
                    continue;

                string dataTypeName = column.SqlDataType != SqlDataType.Unknown ? column.SqlDataType.ToString() : column.DataTypeName;
                if (primaryKey.Name != null && this._primaryKeyColumnLocations.TryGetValue($"{primaryKey.Name}.{column.Name}", out TSqlFragment target))
                    base.FailIfUnsuppressed(target, identifier, actualTableName, column.Name, dataTypeName.ToUpperInvariant());
                else
                    base.FailIfUnsuppressed(column.Source, identifier, actualTableName, column.Name, dataTypeName.ToUpperInvariant());
            }
        }

        private sealed class PrimaryKeyColumnLocationVisitor : TSqlFragmentVisitor
        {
            public IDictionary<string, TSqlFragment> PrimaryKeyColumnLocations { get; }

            public PrimaryKeyColumnLocationVisitor() => this.PrimaryKeyColumnLocations = new Dictionary<string, TSqlFragment>();

            public override void Visit(TableDefinition node)
            {
                foreach (ConstraintDefinition constraint in node.TableConstraints)
                {
                    if (!(constraint is UniqueConstraintDefinition uniqueConstraint) || !uniqueConstraint.IsPrimaryKey) 
                        continue;

                    foreach (ColumnWithSortOrder column in uniqueConstraint.Columns)
                    {
                        if (constraint.ConstraintIdentifier == null) 
                            continue;

                        this.PrimaryKeyColumnLocations.Add($"{constraint.ConstraintIdentifier.Value}.{column.Column.GetName().Value}", column.Column.MultiPartIdentifier);
                    }
                }
            }
        }
    }
}