﻿using System.Collections.Generic;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public sealed class SqlModel
    {
        private readonly TSqlModel _model;

        internal SqlModel(TSqlModel model) => this._model = model;

        public IEnumerable<Constraint> GetConstraints(SchemaObjectName tableName) => this.GetConstraints(TableModel.Table, tableName);
        public IEnumerable<Constraint> GetConstraints(TableModel tableDefinition, SchemaObjectName tableName) => tableDefinition.GetConstraints(this._model, tableName);

        public IEnumerable<Index> GetIndexes(SchemaObjectName tableName) => this.GetIndexes(TableModel.Table, tableName);
        public IEnumerable<Index> GetIndexes(TableModel tableDefinition, SchemaObjectName tableName) => tableDefinition.GetIndexes(this._model, tableName);

        public bool HasPrimaryKey(TableModel tableDefinition, SchemaObjectName tableName) => tableDefinition.HasPrimaryKey(this._model, tableName);
    }
}
