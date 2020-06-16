using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 16)]
    public sealed class UnsupportedDataTypeSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static readonly ICollection<SqlDataTypeOption> ObsoleteDataTypes = new HashSet<SqlDataTypeOption>
        {
            SqlDataTypeOption.NText,
            SqlDataTypeOption.Image
        };
        private static readonly IDictionary<SqlDataTypeOption, HashSet<string>> Suppressions = new Dictionary<SqlDataTypeOption, HashSet<string>>
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
                if (this._tableName != null
                 && Suppressions.TryGetValue(node.SqlDataTypeOption, out HashSet<string> workarounds)
                 && workarounds.Contains(this._tableName))
                {
                    return;
                }
                base.Fail(node, $"The data type '{node.SqlDataTypeOption.ToString().ToUpperInvariant()}' is obsolete and should not be used");
            }
        }
    }
}