using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class ObsoleteDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule<ObsoleteDataTypeSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 16;
        public override string ErrorMessage => "The data type '{0}' is obsolete and should not be used";
    }

    public sealed class ObsoleteDataTypeSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private static readonly HashSet<SqlDataTypeOption> ObsoleteDataTypes = new HashSet<SqlDataTypeOption>
        {
            SqlDataTypeOption.NText,
            SqlDataTypeOption.Image
        };
        private static readonly IDictionary<SqlDataTypeOption, HashSet<string>> Workarounds = new Dictionary<SqlDataTypeOption, HashSet<string>>
        {
            {
                SqlDataTypeOption.Image, new HashSet<string>
                {
                    "hlsysadhocprocessdefinition"
                  , "hlsysdocument"
                  , "hlsyslicexport"
                  , "hlsyslicimport"
                  , "hlsysobjectmodelassembly"
                  , "hlsyssearchresult"
                  , "hlwfactivityproject"
                  , "hlwfcompletedscope"
                  , "hlwfworkflowdefinition"
                }
            }
        };
        private string _tableName;

        public override void Visit(CreateTableStatement node)
        {
            this._tableName = node.SchemaObjectName.BaseIdentifier.Value;
        }

        public override void Visit(SqlDataTypeReference node)
        {
            if (!ObsoleteDataTypes.Contains(node.SqlDataTypeOption))
                return;

            if (this._tableName != null
             && Workarounds.TryGetValue(node.SqlDataTypeOption, out HashSet<string> workarounds)
             && workarounds.Contains(this._tableName))
            {
                return;
            }

            base.Fail(node, node.SqlDataTypeOption.ToString().ToUpperInvariant());
        }
    }
}