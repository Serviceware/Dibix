using System.Collections.Generic;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 39)]
    public sealed class ExplicitProcedureArgumentSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Procedure arguments should be named: {0} = {1}";
        
        public override void Visit(ExecutableProcedureReference node)
        {
            if (node.ProcedureReference.ProcedureReference == null)
                return;

            if (!base.Model.TryGetFunctionParameterNames(node.ProcedureReference.ProcedureReference.Name, out IList<string> parameterNames))
            {
                base.LogError(node.ProcedureReference.ProcedureReference, "71502", $"Cannot resolve reference to object {node.ProcedureReference.ProcedureReference.Name.Dump()}");
                return;
            }

            for (int i = 0; i < node.Parameters.Count; i++)
            {
                ExecuteParameter parameter = node.Parameters[i];
                if (parameter.Variable != null)
                    continue;

                string parameterName = parameterNames[i];
                base.Fail(parameter, parameterName, parameter.ParameterValue.Dump());
            }
        }
    }
}