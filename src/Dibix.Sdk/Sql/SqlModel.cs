﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public sealed class SqlModel
    {
        internal const string DefaultSchemaName = "dbo";
        private readonly TSqlModel _model;
        private readonly TSqlElementLocator _elementLocator;

        internal SqlModel(TSqlModel model, TSqlFragment scriptFragment, bool isScriptArtifact)
        {
            this._model = model;
            this._elementLocator = new TSqlElementLocator(new Lazy<TSqlModel>(() => model), scriptFragment, isScriptArtifact);
        }

        public IEnumerable<Constraint> GetConstraints(SchemaObjectName tableName, bool throwOnError = true) => this.GetConstraints(TableModel.Table, tableName, throwOnError);
        public IEnumerable<Constraint> GetConstraints(TableModel tableDefinition, SchemaObjectName tableName, bool throwOnError = true) => tableDefinition.GetConstraints(this._model, tableName, throwOnError);

        public IEnumerable<Index> GetIndexes(SchemaObjectName tableName) => this.GetIndexes(TableModel.Table, tableName);
        public IEnumerable<Index> GetIndexes(TableModel tableDefinition, SchemaObjectName tableName) => tableDefinition.GetIndexes(this._model, tableName);

        public bool HasPrimaryKey(TableModel tableDefinition, SchemaObjectName tableName) => tableDefinition.HasPrimaryKey(this._model, tableName);

        public bool IsPartOfPrimaryKey(ElementLocation element)
        {
            TSqlObject columnElement = element.GetModelElement(this._model);
            TSqlObject table = columnElement.GetParent(DacQueryScopes.All);
            TSqlObject primaryKey = table.GetReferencing(PrimaryKeyConstraint.Host, DacQueryScopes.All).First();
            return primaryKey?.GetReferenced(PrimaryKeyConstraint.Columns, DacQueryScopes.All).Contains(columnElement) ?? default;
        }

        public bool IsScalarFunction(FunctionCall functionCall) => this._elementLocator.TryGetModelElement(functionCall, out TSqlObject element) && element.ObjectType == ScalarFunction.TypeClass;

        public bool TryGetModelElement(TSqlFragment fragment, out ElementLocation element) => this._elementLocator.TryGetElementLocation(fragment, out element);

        public bool IsDataType(ElementLocation element)
        {
            TSqlObject modelElement = element.GetModelElement(this._model);
            return modelElement?.ObjectType == DataType.TypeClass;
        }
    }
}
