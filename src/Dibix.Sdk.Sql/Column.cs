using System.Diagnostics;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.Sql
{
    [DebuggerDisplay("{Name} {DataTypeName}")]
    public class Column
    {
        public string Name { get; }
        public SqlDataType SqlDataType { get; }
        public string DataTypeName { get; }
        public bool IsNullable { get; }
        public bool IsComputed { get; }
        public int Length { get; }
        public int Precision { get; }
        public SourceInformation Source { get; }

        internal Column(string name, SqlDataType sqlDataType, string dataTypeName, bool isNullable, bool isComputed, int length, int precision, SourceInformation source)
        {
            this.Name = name;
            this.SqlDataType = sqlDataType;
            this.DataTypeName = dataTypeName;
            this.IsNullable = isNullable;
            this.IsComputed = isComputed;
            this.Length = length;
            this.Precision = precision;
            this.Source = source;
        }
    }
}