using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public sealed class SqlUnicodeTypeLintRule : SqlLintRule
    {
        public override int Id => 5;
        public override string ErrorMessage => "Invalid data type VARCHAR. Please use NVARCHAR for unicode";

        public override void Visit(SqlDataTypeReference node)
        {
            if (node.SqlDataTypeOption == SqlDataTypeOption.VarChar)
                base.Fail(node);
        }
    }
}