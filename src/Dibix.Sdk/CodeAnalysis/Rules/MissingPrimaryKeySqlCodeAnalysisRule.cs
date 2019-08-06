using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;

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

        protected override void Visit(Table table)
        {
            if (Workarounds.Contains(table.Name.BaseIdentifier.Value))
                return;

            bool hasPrimaryKey = base.GetConstraints(table.Name).Any(x => x.Type == ConstraintType.PrimaryKey);
            if (!hasPrimaryKey)
                base.Fail(table.Definition, ToDisplayName(table.Type), table.Name.BaseIdentifier.Value);
        }

        private static string ToDisplayName(TableType type)
        {
            switch (type)
            {
                case TableType.Table: return "Table";
                case TableType.TypeTable: return "User defined table type";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}