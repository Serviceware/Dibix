using System.Collections.Generic;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public sealed class SqlModel
    {
        internal const string DefaultSchemaName = "dbo";
        private readonly TSqlModel _model;

        internal SqlModel(TSqlModel model) => this._model = model;

        public IEnumerable<Constraint> GetConstraints(SchemaObjectName tableName, bool throwOnError = true) => this.GetConstraints(TableModel.Table, tableName, throwOnError);
        public IEnumerable<Constraint> GetConstraints(TableModel tableDefinition, SchemaObjectName tableName, bool throwOnError = true) => tableDefinition.GetConstraints(this._model, tableName, throwOnError);

        public IEnumerable<Index> GetIndexes(SchemaObjectName tableName) => this.GetIndexes(TableModel.Table, tableName);
        public IEnumerable<Index> GetIndexes(TableModel tableDefinition, SchemaObjectName tableName) => tableDefinition.GetIndexes(this._model, tableName);

        public bool HasPrimaryKey(TableModel tableDefinition, SchemaObjectName tableName) => tableDefinition.HasPrimaryKey(this._model, tableName);
    }
}
