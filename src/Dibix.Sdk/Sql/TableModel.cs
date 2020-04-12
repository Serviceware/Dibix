using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public abstract class TableModel
    {
        #region Factory Properties
        public static TableModel Table { get; } = new Table();
        public static TableModel TableType { get; } = new TableType();
        #endregion

        #region Properties
        public abstract string TypeDisplayName { get; }
        protected abstract ModelTypeClass ObjectType { get; }
        protected abstract ModelTypeClass ColumnType { get; }
        protected abstract ModelRelationshipClass ColumnDataType { get; }
        protected abstract ModelPropertyClass ColumnLength { get; }
        protected abstract ModelPropertyClass ColumnPrecision { get; }
        protected abstract ModelPropertyClass Nullable { get; }
        #endregion

        #region Public Methods
        public IEnumerable<Constraint> GetConstraints(TSqlModel model, SchemaObjectName tableName, bool throwOnError)
        {
            TSqlObject table = this.GetTable(model, tableName, throwOnError);
            return table != null ? this.GetConstraints(table) : Enumerable.Empty<Constraint>();
        }

        public IEnumerable<Index> GetIndexes(TSqlModel model, SchemaObjectName tableName)
        {
            TSqlObject table = this.GetTable(model, tableName);
            IEnumerable<Index> indexes = table.GetReferencing(Microsoft.SqlServer.Dac.Model.Index.IndexedObject, DacQueryScopes.All)
                                              .Select(this.MapIndex);
            return indexes;
        }

        public bool HasPrimaryKey(TSqlModel model, SchemaObjectName tableName)
        {
            TSqlObject table = this.GetTable(model, tableName);
            return this.HasPrimaryKey(table);
        }
        #endregion

        #region Abstract Methods
        protected abstract bool IsComputed(TSqlObject column);
        protected abstract bool HasPrimaryKey(TSqlObject table);
        protected abstract IEnumerable<Constraint> GetConstraints(TSqlObject table);
        #endregion

        #region Protected Methods
        protected Constraint ToConstraint(ConstraintSelector constraintSelector, TSqlObject constraintModel)
        {
            string name = constraintModel.Name.HasName ? constraintModel.Name.Parts.Last() : null;
            bool? isClustered = constraintSelector.Clustered != null ? constraintModel.GetProperty<bool>(constraintSelector.Clustered) : (bool?)null;
            string checkCondition = (string)(constraintSelector.CheckExpression != null ? constraintModel.GetProperty(constraintSelector.CheckExpression) : null);
            Constraint constraint = new Constraint(constraintSelector.Kind, name, isClustered, constraintModel.GetSourceInformation(), checkCondition);
            if (constraintSelector.Columns != null)
                constraint.Columns.AddRange(constraintModel.GetReferenced(constraintSelector.Columns, DacQueryScopes.All)
                                                           .Where(this.IsColumn)
                                                           .Select(this.MapColumn));

            return constraint;
        }
        #endregion

        #region Private Methods
        private TSqlObject GetTable(TSqlModel model, SchemaObjectName name, bool throwOnError = true)
        {
            ObjectIdentifier id = new ObjectIdentifier(name.Identifiers.Select(x => x.Value));
            if (name.SchemaIdentifier == null)
                id.Parts.Insert(0, SqlModel.DefaultSchemaName);

            TSqlObject table = model.GetObject(this.ObjectType, id, DacQueryScopes.All);
            if (table == null && throwOnError)
                throw new InvalidOperationException($"Could not find table in model: {id}");

            return table;
        }

        private bool IsColumn(TSqlObject model) => model.ObjectType == this.ColumnType;

        private Column MapColumn(TSqlObject model)
        {
            string columnName = model.Name.Parts.Last();
            bool isNullable = model.GetProperty<bool>(this.Nullable);
            bool isComputed = this.IsComputed(model);
            int length = model.GetProperty<int>(this.ColumnLength);
            int precision = model.GetProperty<int>(this.ColumnPrecision);
            SqlDataType sqlDataType = SqlDataType.Unknown;
            string dataTypeName = null;
            if (!isComputed)
            {
                TSqlObject columnType = model.GetReferenced(this.ColumnDataType, DacQueryScopes.All).Single();
                sqlDataType = columnType.GetProperty<SqlDataType>(DataType.SqlDataType);
                dataTypeName = String.Join(".", columnType.Name.Parts);
            }
            Column column = new Column(columnName, sqlDataType, dataTypeName, isNullable, isComputed, length, precision, model.GetSourceInformation());
            return column;
        }

        private Index MapIndex(TSqlObject model)
        {
            string name = model.Name.Parts.Last();
            bool isUnique = model.GetProperty<bool>(Microsoft.SqlServer.Dac.Model.Index.Unique);
            bool isClustered = model.GetProperty<bool>(Microsoft.SqlServer.Dac.Model.Index.Clustered);
            string filter = (string)model.GetProperty(Microsoft.SqlServer.Dac.Model.Index.FilterPredicate);
            Index index = new Index(name, isUnique, isClustered, model.GetSourceInformation(), filter);
            index.Columns.AddRange(model.GetReferenced(Microsoft.SqlServer.Dac.Model.Index.Columns, DacQueryScopes.All).Select(this.MapColumn));
            index.IncludeColumns.AddRange(model.GetReferenced(Microsoft.SqlServer.Dac.Model.Index.IncludedColumns, DacQueryScopes.All).Select(x => x.Name.Parts.Last()));
            return index;
        }
        #endregion

        #region Nested Types
        protected struct ConstraintSelector
        {
            public ConstraintKind Kind { get; }
            public ModelRelationshipClass Host { get; }
            public ModelRelationshipClass Columns { get; }
            public ModelPropertyClass Clustered { get; }
            public ModelPropertyClass CheckExpression { get; }

            public ConstraintSelector(ConstraintKind kind, ModelRelationshipClass columns, ModelPropertyClass clustered, ModelPropertyClass checkExpression) : this(kind, null, columns, clustered, checkExpression) { }
            public ConstraintSelector(ConstraintKind kind, ModelRelationshipClass host, ModelRelationshipClass columns, ModelPropertyClass clustered, ModelPropertyClass checkExpression)
            {
                this.Kind = kind;
                this.Host = host;
                this.Columns = columns;
                this.Clustered = clustered;
                this.CheckExpression = checkExpression;
            }
        }
        #endregion
    }
}