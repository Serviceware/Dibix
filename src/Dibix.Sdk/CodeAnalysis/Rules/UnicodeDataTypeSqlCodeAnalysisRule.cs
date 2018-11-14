﻿using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class UnicodeDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnicodeDataTypeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 5;
        public override string ErrorMessage => "Use unicode data types instead of ascii. Replace '{0}' with '{1}'.";

        protected override void Analyze(TSqlObject modelElement, TSqlFragment scriptFragment)
        {
            // Temporary suppression for HelplineData.dacpac
            if (modelElement?.Name.ToString() == "[dbo].[hlsys_splitstringa]")
                return;

            base.Analyze(modelElement, scriptFragment);
        }
    }

    public sealed class UnicodeDataTypeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(SqlDataTypeReference node)
        {
            if (node.SqlDataTypeOption == SqlDataTypeOption.Char || node.SqlDataTypeOption == SqlDataTypeOption.VarChar)
                base.Fail(node, node.SqlDataTypeOption.ToString().ToUpperInvariant(), $"N{node.SqlDataTypeOption}".ToUpperInvariant());
        }
    }
}