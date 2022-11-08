using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public sealed class SqlModel
    {
        private readonly TSqlModel _model;
        private readonly TSqlFragmentAnalyzer _fragmentAnalyzer;

        public SqlModel(string source, TSqlFragment scriptFragment, bool isScriptArtifact, bool isEmbedded, bool limitDdlStatements, TSqlModel model, ILogger logger)
        {
            this._model = model;
            this._fragmentAnalyzer = TSqlFragmentAnalyzer.Create(source, scriptFragment, isScriptArtifact, isEmbedded, limitDdlStatements, analyzeAlways: true, model, logger);
        }

        public IEnumerable<Constraint> GetTableConstraints(SchemaObjectName tableName, bool throwOnError = true) => this.GetConstraints(TableModel.Table, tableName, throwOnError);
        public IEnumerable<Constraint> GetConstraints(TableModel tableModel, SchemaObjectName tableName, bool throwOnError = true) => tableModel.GetConstraints(this._model, tableName, throwOnError);

        public IEnumerable<Index> GetIndexes(TableModel tableModel, SchemaObjectName tableName) => tableModel.GetIndexes(this._model, tableName);

        public bool HasPrimaryKey(TableModel tableModel, SchemaObjectName tableName) => tableModel.HasPrimaryKeyConstraint(this._model, tableName);

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

        public bool IsScalarFunction(FunctionCall functionCall) => this._fragmentAnalyzer.TryGetModelElement(functionCall, out TSqlObject element) && element.ObjectType == ScalarFunction.TypeClass;

        public bool TryGetModelElement(TSqlFragment fragment, out ElementLocation element) => this._fragmentAnalyzer.TryGetElementLocation(fragment, out element);

        public bool IsDataType(ElementLocation element)
        {
            TSqlObject modelElement = element.GetModelElement(this._model);
            return modelElement?.ObjectType == DataType.TypeClass;
        }

        public bool TryGetFunctionParameterNames(SchemaObjectName functionName, out IList<string> parameterNames)
        {
            if (!this._fragmentAnalyzer.TryGetModelElement(functionName, out TSqlObject function))
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

        public bool IsExternal(ElementLocation element)
        {
            TSqlObject modelElement = element.GetModelElement(this._model);
            return modelElement.IsExternal();
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