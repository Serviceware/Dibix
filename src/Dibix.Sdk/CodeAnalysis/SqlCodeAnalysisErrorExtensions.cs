using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.CodeAnalysis;

namespace Dibix.Sdk.CodeAnalysis
{
    internal static class SqlCodeAnalysisErrorExtensions
    {
        public static SqlRuleProblem ToProblem(this SqlCodeAnalysisError error)
        {
            SqlRuleProblem problem = new SqlRuleProblem(error.Message, error.ModelElement, error.ScriptFragment);
            problem.SetSourceInformation(new SourceInformation(problem.SourceName, error.Line, error.Column));
            return problem;
        }
    }
}
