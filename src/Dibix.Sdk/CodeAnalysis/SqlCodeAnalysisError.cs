using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisError
    {
        public int RuleId { get; }
        public string Message { get; }
        public TSqlFragment ScriptFragment { get; }
        public int Line { get; }
        public int Column { get; }

        public SqlCodeAnalysisError(int ruleId, string message, TSqlFragment scriptFragment, int line, int column)
        {
            this.RuleId = ruleId;
            this.Message = message;
            this.ScriptFragment = scriptFragment;
            this.Line = line;
            this.Column = column;
        }
    }
}