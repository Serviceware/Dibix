using System.Collections.Generic;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 12)]
    public sealed class MissingPrimaryKeySqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        // helpLine suppressions
        // Adding a PK here would be very slow due to the size of the tables
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["hlwfactivityevents"] = "dce47ae78dd11da1d3ae3d995a22e146"
          , ["hlwfinstanceevents"] = "7282a66a03bc842c287dafe48c4d63f3"
          , ["hlwfuserevents"] = "88a7ceb79f31f43c31525ea8cd5a26cc"
        };

        protected override string ErrorMessageTemplate => "{0} '{1}' does not have a primary key";

        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            string name = tableName.BaseIdentifier.Value;
            if (Suppressions.TryGetValue(name, out string hash) && hash == base.Hash)
                return;

            bool hasPrimaryKey = base.Model.HasPrimaryKey(tableModel, tableName);
            if (!hasPrimaryKey)
                base.Fail(tableDefinition, tableModel.TypeDisplayName, name);
        }
    }
}