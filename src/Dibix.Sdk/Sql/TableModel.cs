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
        public abstract ModelTypeClass ObjectType { get; }
        public abstract ModelTypeClass ColumnType { get; }
        public abstract ModelRelationshipClass ColumnDataType { get; }
        public abstract ModelPropertyClass ColumnLength { get; }
        public abstract ModelPropertyClass ColumnPrecision { get; }
        public abstract ModelPropertyClass Nullable { get; }
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
            IEnumerable<Index> indexes = table.GetReferencing(Microsoft.SqlServer.Dac.Model.Index.IndexedObject)
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
            ConstraintDefinition definition = GetConstraintDefinition(constraintModel, name);
            Constraint constraint = new Constraint(constraintSelector.Kind, name, isClustered, constraintModel.GetSourceInformation(), definition);
            if (constraintSelector.Columns != null)
                constraint.Columns?.AddRange(constraintModel.GetReferenced(constraintSelector.Columns)
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

            TSqlObject table = model.GetObject(this.ObjectType, id, DacQueryScopes.UserDefined);
            if (table == null && throwOnError)
                throw new InvalidOperationException($"Could not find table in model: {id}");

            return table;
        }

        private static ConstraintDefinition GetConstraintDefinition(TSqlObject model, string name)
        {
            TSqlFragment scriptDom = model.GetScriptDom();
            switch (scriptDom)
            {
                case AlterTableAddTableElementStatement alterTableAddTableElementStatement:
                    return alterTableAddTableElementStatement.Definition
                                                             .TableConstraints
                                                             .Single(x => x.ConstraintIdentifier.Value == name);

                case ConstraintDefinition constraintDefinition:
                    return constraintDefinition;

                default:
                    throw new ArgumentOutOfRangeException(nameof(scriptDom), scriptDom, null);
            }
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
                TSqlObject columnType = model.GetReferenced(this.ColumnDataType).Single();
                sqlDataType = columnType.GetProperty<SqlDataType>(DataType.SqlDataType);
                dataTypeName = String.Join(".", columnType.Name.Parts);
            }
            TSqlFragment scriptDom = model.GetScriptDom();
            Column column = new Column(columnName, sqlDataType, dataTypeName, isNullable, isComputed, length, precision, scriptDom);
            return column;
        }

        private Index MapIndex(TSqlObject model)
        {
            string name = model.Name.Parts.Last();
            bool isUnique = model.GetProperty<bool>(Microsoft.SqlServer.Dac.Model.Index.Unique);
            bool isClustered = model.GetProperty<bool>(Microsoft.SqlServer.Dac.Model.Index.Clustered);
            TSqlFragment definition = model.GetScriptDom();
            Identifier identifier = ExtractIndexIdentifier(definition);
            Index index = new Index(name, isUnique, isClustered, model.GetSourceInformation(), identifier, definition);
            index.Columns?.AddRange(model.GetReferenced(Microsoft.SqlServer.Dac.Model.Index.Columns).Select(this.MapColumn));
            return index;
        }

        private static Identifier ExtractIndexIdentifier(TSqlFragment fragment)
        {
            switch (fragment)
            {
                case CreateIndexStatement createIndexStatement: return createIndexStatement.Name;
                case IndexStatement indexStatement: return indexStatement.Name;
                case IndexDefinition indexDefinition: return indexDefinition.Name;
                default: throw new ArgumentOutOfRangeException(nameof(fragment), fragment, null);
            }
        }
        #endregion

        #region Nested Types
        protected struct ConstraintSelector
        {
            public ConstraintKind Kind { get; }
            public ModelRelationshipClass Host { get; }
            public ModelRelationshipClass Columns { get; }
            public ModelPropertyClass Clustered { get; }

            public ConstraintSelector(ConstraintKind kind, ModelRelationshipClass columns, ModelPropertyClass clustered) : this(kind, null, columns, clustered) { }
            public ConstraintSelector(ConstraintKind kind, ModelRelationshipClass host, ModelRelationshipClass columns, ModelPropertyClass clustered)
            {
                this.Kind = kind;
                this.Host = host;
                this.Columns = columns;
                this.Clustered = clustered;
            }
        }
        #endregion
    }
}