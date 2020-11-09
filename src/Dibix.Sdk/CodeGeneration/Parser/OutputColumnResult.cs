using System;
using Dibix.Sdk.Sql;
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
        public Lazy<SqlDataType> DataTypeAccessor { get; }

        public OutputColumnResult(string columnName, TSqlFragment primarySource, TSqlFragment columnNameSource, TSqlFragmentAnalyzer fragmentAnalyzer)
        {
            this.HasName = !String.IsNullOrEmpty(columnName);
            this.ColumnName = columnName;
            this.ColumnNameSource = columnNameSource;
            this.PrimarySource = primarySource;
            this.DataTypeAccessor = new Lazy<SqlDataType>(() => primarySource.GetDataType(fragmentAnalyzer));
        }
    }
}