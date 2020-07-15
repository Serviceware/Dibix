using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public sealed class SqlModel
    {
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

        public bool HasPrimaryKey(TableModel tableDefinition, SchemaObjectName tableName) => tableDefinition.HasPrimaryKeyConstraint(this._model, tableName);

        public bool? IsPartOfPrimaryKey(ElementLocation element)
        {
            TSqlObject columnElement = element.GetModelElement(this._model);
            if (columnElement == null)
                return null;

            TSqlObject table = columnElement.GetParent(DacQueryScopes.All);
            TSqlObject primaryKey = table.GetReferencing(PrimaryKeyConstraint.Host, DacQueryScopes.All).First();
            return primaryKey?.GetReferenced(PrimaryKeyConstraint.Columns, DacQueryScopes.All).Contains(columnElement) ?? default;
        }

        public IDictionary<string, bool> GetUserDefinedTableTypeColumnsWithPrimaryKeyInformation(ElementLocation element)
        {
            TSqlObject modelElement = element.GetModelElement(this._model);
            if (modelElement == null)
                return null;

            if (modelElement.ObjectType != ModelSchema.TableType)
                return null;

            return GetColumnsWithPrimaryKeyInformation(modelElement).ToDictionary(x => x.Key, x => x.Value);
        }

        public bool IsScalarFunction(FunctionCall functionCall) => this._elementLocator.TryGetModelElement(functionCall, out TSqlObject element) && element.ObjectType == ScalarFunction.TypeClass;

        public bool TryGetModelElement(TSqlFragment fragment, out ElementLocation element) => this._elementLocator.TryGetElementLocation(fragment, out element);

        public bool IsDataType(ElementLocation element)
        {
            TSqlObject modelElement = element.GetModelElement(this._model);
            return modelElement?.ObjectType == DataType.TypeClass;
        }

        public bool TryGetFunctionParameterNames(SchemaObjectName functionName, out IList<string> parameterNames)
        {
            if (!this._elementLocator.TryGetModelElement(functionName, out TSqlObject function))
            {
                parameterNames = null;
                return false;
            }

            ModelRelationshipClass parametersRelationship = GetParametersRelationship(function.ObjectType);
            parameterNames = function.GetReferenced(parametersRelationship, DacQueryScopes.All)
                                     .Select(x => x.Name.Parts.Last())
                                     .ToArray();

            return true;
        }

        private static IEnumerable<KeyValuePair<string, bool>> GetColumnsWithPrimaryKeyInformation(TSqlObject modelElement)
        {
            TableModel tableModel = TableModel.GetAccessor(modelElement.ObjectType);
            return tableModel.GetColumnsWithPrimaryKeyInformation(modelElement);
        }

        private static ModelRelationshipClass GetParametersRelationship(ModelTypeClass type)
        {
            if (type == ModelSchema.Procedure)
                return Procedure.Parameters;

            if (type == ModelSchema.ExtendedProcedure)
                return Procedure.Parameters;

            if (type == ModelSchema.ScalarFunction)
                return ScalarFunction.Parameters;

            if (type == ModelSchema.TableValuedFunction)
                return TableValuedFunction.Parameters;
            
            throw new ArgumentOutOfRangeException($"Unexpected function type: {type}");
        }
    }
}