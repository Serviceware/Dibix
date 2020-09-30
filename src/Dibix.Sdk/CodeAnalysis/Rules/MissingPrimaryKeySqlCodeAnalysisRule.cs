using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 12)]
    public sealed class MissingPrimaryKeySqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "{0} '{1}' does not have a primary key";

        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            string name = tableName.BaseIdentifier.Value;
            bool hasPrimaryKey = base.Model.HasPrimaryKey(tableModel, tableName);

            if (!hasPrimaryKey)
                base.FailIfUnsuppressed(tableDefinition, name, tableModel.TypeDisplayName, name);
        }
    }
}