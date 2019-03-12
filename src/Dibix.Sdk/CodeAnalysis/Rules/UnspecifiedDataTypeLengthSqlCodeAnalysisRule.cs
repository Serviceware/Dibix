using System.Collections.Generic;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UnspecifiedDataTypeLengthSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnspecifiedDataTypeLengthSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 9;
        public override string ErrorMessage => "Data type length not specified: {0}";
    }

    public sealed class UnspecifiedDataTypeLengthSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private static readonly HashSet<SqlDataTypeOption> DataTypesThatRequireLength = new HashSet<SqlDataTypeOption>
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