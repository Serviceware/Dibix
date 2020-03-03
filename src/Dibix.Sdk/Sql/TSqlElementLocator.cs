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

        public TSqlElementLocator(Lazy<TSqlModel> modelAccessor, TSqlFragment sqlFragment, string namingConventionPrefix, bool isScriptArtifact)
        {
            this._modelAccessor = modelAccessor;
            this._elementLocationsAccessor = new Lazy<IDictionary<int, ElementLocation>>(() => AnalyzeSchema(modelAccessor.Value, sqlFragment, namingConventionPrefix, isScriptArtifact).Locations.ToDictionary(x => x.Offset));
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

        private static SchemaAnalyzerResult AnalyzeSchema(TSqlModel model, TSqlFragment sqlFragment, string namingConventionPrefix, bool isScriptArtifact)
        {
            SchemaAnalyzerResult schemaAnalyzerResult = SchemaAnalyzer.Analyze(model, sqlFragment);

            // Loading the model of a PreDeploy/PostDeploy script is not officially supported, that's why these scripts aren't analyzed in the SQL projects.
            // We still wan't to be able to ensure our guidelines/patterns and apply our analysis rules to these artifacts aswell.
            // Therefore we have to implement a couple of workarounds.
            if (isScriptArtifact)
            {
                // 'The statement cannot be a top-level statement' for DeclareTableVariableStatement
                // Just wrap the whole script in an SP, to make it a DDL statement
                if (schemaAnalyzerResult.Errors.Any(x => x.ErrorCode == 70501))
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

                // 'Warning 70588: WITH CHECK | NOCHECK option for existing data check enforcement is ignored'
                // This rule was designed for compilable DDL artifacts and keeping in mind, that the project is in the ideal desired state of the DDL schema.
                // So it's saying here that this statement is essentially meaningless, because when the dacpac is being published,
                // these constraints will be enforced during the check phase at the end.
                // Since we are in a PreDeploy/PostDeploy script, we can safely suppress it here.
                foreach (ExtensibilityError withCheckError in schemaAnalyzerResult.Errors.Where(x => x.ErrorCode == 70588).ToArray())
                    schemaAnalyzerResult.Errors.Remove(withCheckError);
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