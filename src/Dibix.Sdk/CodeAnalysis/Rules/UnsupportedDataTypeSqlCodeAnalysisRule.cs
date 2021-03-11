using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 16)]
    public sealed class UnsupportedDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        // See: https://docs.microsoft.com/en-us/sql/t-sql/data-types/ntext-text-and-image-transact-sql
        private static readonly ICollection<SqlDataTypeOption> ObsoleteDataTypes = new HashSet<SqlDataTypeOption>
        {
            SqlDataTypeOption.Text,
            SqlDataTypeOption.NText,
            SqlDataTypeOption.Image
        };
        private string _tableName;

        protected override string ErrorMessageTemplate => "{0}";

        public override void Visit(CreateTableStatement node)
        {
            this._tableName = node.SchemaObjectName.BaseIdentifier.Value;
        }

        public override void Visit(SqlDataTypeReference node)
        {
            if (node.SqlDataTypeOption == SqlDataTypeOption.DateTime2)
            {
                base.Fail(node, "Please use DATETIME instead of DATETIME2");
                return;
            }

            if (ObsoleteDataTypes.Contains(node.SqlDataTypeOption))
            {
                string errorMessage = $"The data type '{node.SqlDataTypeOption.ToString().ToUpperInvariant()}' is obsolete and should not be used";
                
                if (this._tableName != null)
                {
                    string suppressionKey = $"{node.SqlDataTypeOption}_{this._tableName}";
                    base.FailIfUnsuppressed(node, suppressionKey, errorMessage);
                }
                else
                    base.Fail(node, errorMessage);
            }
        }
    }
}