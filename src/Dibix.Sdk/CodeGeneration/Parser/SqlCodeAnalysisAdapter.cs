﻿using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration.Parser
{
    internal sealed class SqlCodeAnalysisGeneratorAdapter
    {
        private readonly ISqlCodeAnalysisRuleEngine _engine;

        public SqlCodeAnalysisGeneratorAdapter()
        {
            this._engine = new SqlCodeAnalysisRuleEngine();
        }

        public bool Analyze(IExecutionEnvironment environment, TSqlFragment scriptFragment, string sourceFilePath)
        {
            ICollection<SqlCodeAnalysisError> errors = this._engine.Analyze(null, scriptFragment).ToArray();
            if (!errors.Any())
                return false;

            foreach (SqlCodeAnalysisError error in errors)
                environment.RegisterError(sourceFilePath, error.Line, error.Column, error.RuleId.ToString(), $"SRDBX : Dibix : {error.Message}");

            return true;
        }
    }
}