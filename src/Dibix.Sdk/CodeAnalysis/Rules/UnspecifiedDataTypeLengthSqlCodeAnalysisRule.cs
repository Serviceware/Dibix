using System.Collections.Generic;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 9)]
    public sealed class UnspecifiedDataTypeLengthSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static readonly ICollection<SqlDataTypeOption> DataTypesThatRequireLength = new HashSet<SqlDataTypeOption>
        {
            SqlDataTypeOption.Char,
            SqlDataTypeOption.VarChar,
            SqlDataTypeOption.NVarChar,
            SqlDataTypeOption.NChar,
            SqlDataTypeOption.Binary,
            SqlDataTypeOption.VarBinary,
            SqlDataTypeOption.Decimal,
            SqlDataTypeOption.Numeric,
            SqlDataTypeOption.Float
        };

        protected override string ErrorMessageTemplate => "Data type length not specified: {0}";

        public UnspecifiedDataTypeLengthSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(SqlDataTypeReference node)
        {
            if (node.Parameters.Count > 0)
                return;

            if (DataTypesThatRequireLength.Contains(node.SqlDataTypeOption))
            {
                base.Fail(node, node.Dump());
            }
        }
    }
}