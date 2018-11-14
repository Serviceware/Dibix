using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisError
    {
        public int RuleId { get; }
        public string Message { get; }
        public TSqlObject ModelElement { get; }
        public TSqlFragment ScriptFragment { get; }
        public int Line { get; }
        public int Column { get; }

        public SqlCodeAnalysisError(int ruleId, string message, TSqlObject modelElement, TSqlFragment scriptFragment, int line, int column)
        {
            this.RuleId = ruleId;
            this.Message = message;
            this.ModelElement = modelElement;
            this.ScriptFragment = scriptFragment;
            this.Line = line;
            this.Column = column;
        }
    }
}