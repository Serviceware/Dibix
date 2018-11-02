using System.Collections;

namespace Dibix.Sdk
{
    internal sealed class OutputColumnResult
    {
        public bool Result { get; private set; }
        public string ColumnName { get; private set; }
        public string Expression { get; private set; }
        public int Line { get; set; }
        public int Column { get; set; }

        private OutputColumnResult(bool result, string columnName, string expression, int line, int column)
        {
            this.Result = result;
            this.ColumnName = columnName;
            this.Expression = expression;
            this.Line = line;
            this.Column = column;
        }

        public static OutputColumnResult Success(string columnName, int line, int column)
        {
            return new OutputColumnResult(true, columnName, null, line, column);
        }

        public static OutputColumnResult Fail(string expression, int line, int column)
        {
            return new OutputColumnResult(false, null, expression, line, column);
        }
    }
}