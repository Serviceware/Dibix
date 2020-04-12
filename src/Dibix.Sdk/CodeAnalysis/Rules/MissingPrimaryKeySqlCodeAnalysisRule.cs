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
            ["hlwfactivityevents"] = "72ba2dc4e62d308ba557ffc9cd634b7b"
          , ["hlwfinstanceevents"] = "f0446a332d61e5557b635cc3090f9b89"
          , ["hlwfuserevents"] = "41fa48d0ecec2be844f4c45a416f9615"
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