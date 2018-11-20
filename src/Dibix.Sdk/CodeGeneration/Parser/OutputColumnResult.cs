﻿namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class OutputColumnResult
    {
        public bool Result { get; }
        public string ColumnName { get; }
        public string Expression { get; }
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