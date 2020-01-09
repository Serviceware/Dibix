using System.Collections.Generic;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class MissingPrimaryKeySqlCodeAnalysisRule : SqlCodeAnalysisRule<MissingPrimaryKeySqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 12;
        public override string ErrorMessage => "{0} '{1}' does not have a primary key";
    }

    public sealed class MissingPrimaryKeySqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        // helpLine suppressions
        // Adding a PK here would be very slow due to the size of the tables
        private static readonly ICollection<string> Workarounds = new HashSet<string>
        {
            "hlwfactivityevents"
          , "hlwfinstanceevents"
          , "hlwfuserevents"
        };

        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            if (Workarounds.Contains(tableName.BaseIdentifier.Value))
                return;

            bool hasPrimaryKey = base.Model.HasPrimaryKey(tableModel, tableName);
            if (!hasPrimaryKey)
                base.Fail(tableDefinition, tableModel.TypeDisplayName, tableName.BaseIdentifier.Value);
        }
    }
}