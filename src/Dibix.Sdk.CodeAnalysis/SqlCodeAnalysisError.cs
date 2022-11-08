namespace Dibix.Sdk.CodeAnalysis
{
    public sealed class SqlCodeAnalysisError
    {
        public int RuleId { get; }
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }

        public SqlCodeAnalysisError(int ruleId, string message, int line, int column)
        {
            this.RuleId = ruleId;
            this.Message = message;
            this.Line = line;
            this.Column = column;
        }
    }
}