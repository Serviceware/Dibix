using System;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.Sql
{
    public sealed class TSqlFragmentAnalyzer
    {
        private readonly string _source;
        private readonly TSqlFragment _sqlFragment;
        private readonly bool _isScriptArtifact;
        private readonly bool _isEmbedded;
        private readonly bool _limitDdlStatements;
        private readonly TSqlModel _model;
        private readonly ILogger _logger;
        private SchemaAnalyzerResult _result;

        private TSqlFragmentAnalyzer(string source, TSqlFragment sqlFragment, bool isScriptArtifact, bool isEmbedded, bool limitDdlStatements, TSqlModel model, ILogger logger)
        {
            this._source = source;
            this._sqlFragment = sqlFragment;
            this._isScriptArtifact = isScriptArtifact;
            this._isEmbedded = isEmbedded;
            this._limitDdlStatements = limitDdlStatements;
            this._model = model;
            this._logger = logger;
        }

        public static TSqlFragmentAnalyzer Create(string source, TSqlFragment sqlFragment, bool isScriptArtifact, bool isEmbedded, bool limitDdlStatements, bool analyzeAlways, TSqlModel model, ILogger logger)
        {
            TSqlFragmentAnalyzer analyzer = new TSqlFragmentAnalyzer(source, sqlFragment, isScriptArtifact, isEmbedded, limitDdlStatements, model, logger);

            // Currently we always analyze. This ensures validating DML projects properly.
            // It is however prepared to be loaded in a lazy manner.
            // So if performance issues arise, optimizations can be evaluated and applied.
            if (analyzeAlways)
                _ = analyzer.GetResult();

            return analyzer;
        }

        public bool TryGetElementLocation(TSqlFragment fragment, out ElementLocation location) => this.GetResult().Locations.TryGetValue(fragment.StartOffset, out location);
        
        public bool TryGetModelElement(TSqlFragment fragment, out TSqlObject element)
        {
            if (this.GetResult().Locations.TryGetValue(fragment.StartOffset, out ElementLocation location))
            {
                element = location.GetModelElement(this._model);
                return element != null;
            }

            element = null;
            return false;
        }

        private SchemaAnalyzerResult GetResult() => this._result ??= AnalyzeSchema();

        private SchemaAnalyzerResult AnalyzeSchema()
        {
            SchemaAnalyzerResult schemaAnalyzerResult = SchemaAnalyzer.Analyze(_model, this._sqlFragment);

            // Loading the model of a PreDeploy/PostDeploy script is not officially supported, that's why these scripts aren't analyzed in the SQL projects.
            // We still want to be able to ensure our guidelines/patterns and apply our analysis rules to these artifacts as well.
            // Therefore, we have to implement a couple of workarounds.
            if (this._isScriptArtifact)
            {
                // 'The statement cannot be a top-level statement' for DeclareTableVariableStatement
                // Just wrap the whole script in an SP, to make it a DDL statement
                if (schemaAnalyzerResult.Errors.Any(x => x.ErrorCode == 70501))
                {
                    if (_sqlFragment is not TSqlScript script)
                        throw new InvalidOperationException("Unable to determine DML statement from script artifact");

                    CreateProcedureStatement proc = new CreateProcedureStatement
                    {
                        ProcedureReference = new ProcedureReference { Name = new SchemaObjectName { Identifiers = { new Identifier { Value = "<dbx_scriptwrapper>" } } } },
                        StatementList = new StatementList()
                    };
                    proc.StatementList.Statements.AddRange(script.Batches.SelectMany(x => x.Statements));

                    schemaAnalyzerResult = SchemaAnalyzer.Analyze(_model, proc);
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

            if (this._isEmbedded)
            {
                foreach (TSqlFragment statement in schemaAnalyzerResult.DDLStatements)
                {
                    if (this.IsSupportedDdlStatement(statement))
                        continue;

                    this._logger.LogError("Only CREATE PROCEDURE is a supported DDL statement within a DML project", this._source, statement.StartLine, statement.StartColumn);
                }
            }

            return schemaAnalyzerResult;
        }

        private bool IsSupportedDdlStatement(TSqlFragment fragment)
        {
            if (!this._limitDdlStatements)
                return true;

            // DML body is always defined in CREATE PROCEDURE
            if (fragment is CreateProcedureStatement)
                return true;

            // We support table variables
            if (fragment is DeclareTableVariableStatement)
                return true;

            // We support NOCHECK/CHECK for CHECK constraints
            if (fragment is AlterTableConstraintModificationStatement constraintModificationStatement && constraintModificationStatement.ConstraintEnforcement != ConstraintEnforcement.NotSpecified)
                return true;
            
            return false;
        }

        private static Exception ToException(ExtensibilityError error)
        {
            return new InvalidOperationException($"{error.Document}({error.Line},{error.Column}) : {error.Severity} {error.ErrorCode}: {error.Message}", error.Exception);
        }
    }
}