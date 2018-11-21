using System.Collections.Generic;
using System.Linq;
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
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
            "hlwfactivityevents"
          , "hlwfinstanceevents"
          , "hlwfuserevents"
        };

        public override void Visit(CreateTableStatement node) => this.Check("Table", node.SchemaObjectName, node.Definition);

        public override void Visit(CreateTypeTableStatement node) => this.Check("User defined table type", node.Name, node.Definition);

        private void Check(string type, SchemaObjectName name, TableDefinition definition)
        {
            if (Workarounds.Contains(name.BaseIdentifier.Value))
                return;

            bool hasPrimaryKey = definition.CollectConstraints().Any(x => x.Type == ConstraintType.PrimaryKey);
            if (!hasPrimaryKey)
                base.Fail(definition, type, name.BaseIdentifier.Value);
        }
    }
}