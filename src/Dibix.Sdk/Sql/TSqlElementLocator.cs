using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    internal sealed class TSqlElementLocator
    {
        private readonly Lazy<IDictionary<int, ElementLocation>> _elementLocationsAccessor;
        private readonly Lazy<TSqlModel> _modelAccessor;

        public TSqlElementLocator(string namingConventionPrefix, Lazy<TSqlModel> modelAccessor, TSqlFragment sqlFragment)
        {
            this._modelAccessor = modelAccessor;
            this._elementLocationsAccessor = new Lazy<IDictionary<int, ElementLocation>>(() => AnalyzeSchema(namingConventionPrefix, modelAccessor.Value, sqlFragment).Locations.ToDictionary(x => x.Offset));
        }

        public bool TryGetElementLocation(TSqlFragment fragment, out ElementLocation location) => this._elementLocationsAccessor.Value.TryGetValue(fragment.StartOffset, out location);
        
        public bool TryGetModelElement(TSqlFragment fragment, out TSqlObject element)
        {
            if (this._elementLocationsAccessor.Value.TryGetValue(fragment.StartOffset, out ElementLocation location))
            {
                element = location.GetModelElement(this._modelAccessor.Value);
                return element != null;
            }

            element = null;
            return false;
        }

        private static SchemaAnalyzerResult AnalyzeSchema(string namingConventionPrefix, TSqlModel model, TSqlFragment sqlFragment)
        {
            SchemaAnalyzerResult schemaAnalyzerResult = SchemaAnalyzer.Analyze(model, sqlFragment);

            // The DACFX model can only compile DDL artifacts
            // This rule does not apply to artifacts with the special build action 'PreDeploy' or 'PostDeploy'
            // To make it work we just make it a DDL statement by wrapping it in an SP
            if (schemaAnalyzerResult.Errors.Any(x => x.ErrorCode == 70501)) // 'The statement cannot be a top-level statement' for DeclareTableVariableStatement
            {
                if (!(sqlFragment is TSqlScript script) || script.Batches.Count != 1)
                    throw new InvalidOperationException("Unable to determine DML statement from script artifact");
                
                CreateProcedureStatement proc = new CreateProcedureStatement
                {
                    ProcedureReference = new ProcedureReference { Name = new SchemaObjectName { Identifiers = { new Identifier { Value = $"{namingConventionPrefix}_scriptwrapper" } } } },
                    StatementList = new StatementList()
                };
                proc.StatementList.Statements.AddRange(script.Batches[0].Statements);

                schemaAnalyzerResult = SchemaAnalyzer.Analyze(model, proc);
            }

            if (schemaAnalyzerResult.Errors.Any())
                throw new AggregateException("One or more errors occured while validating model schema", schemaAnalyzerResult.Errors.Select(ToException));

            return schemaAnalyzerResult;
        }

        private static Exception ToException(ExtensibilityError error)
        {
            return new InvalidOperationException($"{error.Document}({error.Line},{error.Column}) : {error.Severity} {error.ErrorCode}: {error.Message}", error.Exception);
        }
    }
}