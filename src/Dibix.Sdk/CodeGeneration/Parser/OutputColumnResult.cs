using System;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class OutputColumnResult
    {
        public bool HasName { get; }
        public string ColumnName { get; }
        public TSqlFragment PrimarySource { get; }
        public TSqlFragment ColumnNameSource { get; }
        public SqlDataType DataType { get; }
        public bool? IsNullable { get; }

        public OutputColumnResult(string columnName, TSqlFragment primarySource, TSqlFragment columnNameSource, SqlDataType dataType, bool? isNullable)
        {
            this.HasName = !String.IsNullOrEmpty(columnName);
            this.ColumnName = columnName;
            this.ColumnNameSource = columnNameSource;
            this.DataType = dataType;
            this.IsNullable = isNullable;
            this.PrimarySource = primarySource;
        }
    }
}