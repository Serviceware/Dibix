using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisRuleEngine
    {
        #region Fields
        private readonly ICollection<ISqlCodeAnalysisRule> _rules;
        #endregion

        #region Constructor
        public SqlCodeAnalysisRuleEngine()
        {
            this._rules = ScanRules().ToArray();
        }
        #endregion

        #region Public Methods
        public IEnumerable<SqlRuleProblem> Analyze(SqlRuleExecutionContext context)
        {
            SourceInformation sourceInformation = context.ModelElement.GetSourceInformation();

            // Possibly referenced dacpac
            // Since each SQLPROJ should be analyzed separately, we don't need to analyze it again
            if (sourceInformation == null)
                return new SqlRuleProblem[0];

            return this._rules.SelectMany(x => x.Analyze(context));
        }
        #endregion

        #region Private Methods
        private static IEnumerable<ISqlCodeAnalysisRule> ScanRules()
        {
            Type ruleDefinitionType = typeof(ISqlCodeAnalysisRule);
            return ruleDefinitionType.Assembly
                                     .GetTypes()
                                     .Where(ruleDefinitionType.IsAssignableFrom)
                                     .Except(new[] { ruleDefinitionType, typeof(SqlCodeAnalysisRule<>) })
                                     .Select(x => (ISqlCodeAnalysisRule)Activator.CreateInstance(x));
        }
        #endregion
    }
}