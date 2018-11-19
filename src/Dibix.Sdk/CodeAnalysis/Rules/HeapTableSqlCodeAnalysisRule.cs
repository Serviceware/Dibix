using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class HeapTableSqlCodeAnalysisRule : SqlCodeAnalysisRule<HeapTableSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 12;
        public override string ErrorMessage => "Table '{0}' does not have a primary key";
    }

    public sealed class HeapTableSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private static readonly HashSet<string> Workarounds = new HashSet<string>
        {
            "hlwfactivityevents"
          , "hlwfinstanceevents"
          , "hlwfuserevents"
        };

        public override void Visit(CreateTableStatement node)
        {
            // Temporary suppression for HelplineData.dacpac
            if (Workarounds.Contains(node.SchemaObjectName.BaseIdentifier.Value))
                return;

            bool hasTableConstraint = node.Definition.TableConstraints.OfType<UniqueConstraintDefinition>().Any(x => x.IsPrimaryKey);
            bool hasColumnConstraint = node.Definition.ColumnDefinitions.Any(x => x.Constraints.OfType<UniqueConstraintDefinition>().Any(y => y.IsPrimaryKey));
            if (!hasTableConstraint && !hasColumnConstraint)
                base.Fail(node, node.SchemaObjectName.BaseIdentifier.Value);
        }
    }
}