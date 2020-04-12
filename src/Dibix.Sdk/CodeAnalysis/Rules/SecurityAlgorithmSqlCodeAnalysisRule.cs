using System;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 30)]
    public sealed class SecurityAlgorithmSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static readonly string[] SupportedAlgorithms =
        {
            "SHA2_512"
        };
        private static readonly string SupportedAlgorithmsCombined = String.Join(", ", SupportedAlgorithms);

        protected override string ErrorMessageTemplate => "Found use of old security algorithm '{0}'. Please use any of these algorithms: {1}";

        public override void Visit(FunctionCall node)
        {
            if (String.Equals(node.FunctionName.Value, "HASHBYTES", StringComparison.OrdinalIgnoreCase)
             && node.Parameters.Any()
             && node.Parameters[0] is StringLiteral algorithm
             && !SupportedAlgorithms.Contains(algorithm.Value, StringComparer.OrdinalIgnoreCase))
            {
                base.Fail(node, algorithm.Value, SupportedAlgorithmsCombined);
            }
        }
    }
}