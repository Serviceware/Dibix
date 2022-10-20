using System.Collections.Generic;
using System.Text;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 39)]
    public sealed class ExplicitProcedureArgumentSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Procedure arguments should be named: {0} {1} = {2}";

        public ExplicitProcedureArgumentSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(ExecuteSpecification node)
        {
            if (!(node.ExecutableEntity is ExecutableProcedureReference procedureReference))
                return;

            IList<string> parameterNames = null;
            if (procedureReference.ProcedureReference.ProcedureReference != null && !base.Model.TryGetFunctionParameterNames(procedureReference.ProcedureReference.ProcedureReference.Name, out parameterNames))
            {
                base.LogError(procedureReference.ProcedureReference.ProcedureReference, "SQL71502", $"Cannot resolve reference to object {procedureReference.ProcedureReference.ProcedureReference.Name.Dump()}");
                return;
            }

            string executeCall = ExtractExecuteCall(node, procedureReference);
            for (int i = 0; i < procedureReference.Parameters.Count; i++)
            {
                ExecuteParameter parameter = procedureReference.Parameters[i];
                if (parameter.Variable != null)
                    continue;

                string parameterName = parameterNames != null ? parameterNames[i] : $"[{i}]";
                base.Fail(parameter, executeCall, parameterName, parameter.ParameterValue.Dump());
            }
        }

        private static string ExtractExecuteCall(ExecuteSpecification node, ExecutableProcedureReference procedureReference)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = node.FirstTokenIndex; i <= procedureReference.ProcedureReference.LastTokenIndex; i++) 
                sb.Append(node.ScriptTokenStream[i].Text);

            return sb.ToString();
        }
    }
}