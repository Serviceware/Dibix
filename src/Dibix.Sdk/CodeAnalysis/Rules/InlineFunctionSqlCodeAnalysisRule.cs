using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class InlineFunctionSqlCodeAnalysisRule : SqlCodeAnalysisRule<InlineFunctionSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 37;
        public override string ErrorMessage => "{0}";
    }

    public sealed class InlineFunctionSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private const bool AllowNonInlineTableValuedFunctions = false;
        private readonly IDictionary<int, FunctionCall> _scalarFunctionCalls = new Dictionary<int, FunctionCall>();

        // helpLine suppressions
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["fnSplit"] = "fed12bdc66c86181947d415d57434dbf"
          , ["hlsysattachment_query_data_case"] = "0d277c0c11a280b65c491e90a7b73896"
          , ["hlsysattachment_query_data_contract"] = "086a6f84c1fe6810c42e7e61f38e608d"
          , ["hlsysattachment_query_data_person"] = "f3daf9ac7d4da7e3acca5db139abe658"
          , ["hlsysattachment_query_data_product"] = "130048ee6e980dc4271de293e5515695"
          , ["hlsysattachment_query_data_orgunit"] = "42e2f30edb824b44014b96c1803538d4"
          , ["hlsysattachment_query_data_su"] = "07dc107c4d6858c2efbb9fd078e8ff4e"
          , ["hlsysgetusercontext"] = "94acc750635c6a6a1dd347da2b666b87"
          , ["hlsyssec_query_agentsystemacl"] = "b4cea7900ab08592c536357724499706"
          , ["hlsysum_getcentraladminorgunits"] = "ae9cdb9a23cc885575b24a9d0382bd93"
          , ["hltm_getreceiversfortask"] = "c9d9f72724b5722216e1bc9f6415efbf"
        };

        protected override void BeginStatement(TSqlScript node)
        {
            ScalarFunctionCallVisitor visitor = new ScalarFunctionCallVisitor(this.IsScalarFunctionCall);
            node.Accept(visitor);
            this._scalarFunctionCalls.AddRange(visitor.Locations.ToDictionary(x => x.StartOffset));
        }

        protected override void EndStatement(TSqlScript node)
        {
            foreach (FunctionCall scalarFunctionCall in this._scalarFunctionCalls.Values)
            {
                base.Fail(scalarFunctionCall, $"Scalar functions should only be used for assignments or check constraints. Otherwise replace it with an inline table-valued function: {scalarFunctionCall.FunctionName.Value}");
            }
        }

        public override void Visit(CreateFunctionStatement node)
        {
            if (!(node.ReturnType is TableValuedFunctionReturnType) || AllowNonInlineTableValuedFunctions) 
                return;

            string name = node.Name.BaseIdentifier.Value;
            if (Suppressions.TryGetValue(name, out string hash) && hash == base.Hash) 
                return;

            base.Fail(node, $"Make non inline table valued function inline or replace it with a stored procedure: {name}");
        }

        public override void Visit(CheckConstraintDefinition node)
        {
            ScalarFunctionCallVisitor visitor = new ScalarFunctionCallVisitor(this.IsScalarFunctionCall);
            node.CheckCondition.Accept(visitor);
            foreach (FunctionCall call in visitor.Locations)
            {
                if (this._scalarFunctionCalls.ContainsKey(call.StartOffset))
                    this._scalarFunctionCalls.Remove(call.StartOffset);
            }
        }

        public override void Visit(DeclareVariableElement node)
        {
            if (node.Value is ScalarSubquery scalarSubquery
             && scalarSubquery.QueryExpression is QuerySpecification querySpecification)
                this.VisitQuerySpecification(querySpecification);
        }

        public override void Visit(SelectStatement node)
        {
            if (node.QueryExpression is QuerySpecification querySpecification)
                this.VisitQuerySpecification(querySpecification);
        }

        public override void Visit(SetVariableStatement node)
        {
            TSqlFragment target = node?.Expression;
            if (target == null)
                return;

            if (this._scalarFunctionCalls.ContainsKey(target.StartOffset))
                this._scalarFunctionCalls.Remove(target.StartOffset);
        }

        private void VisitQuerySpecification(QuerySpecification node)
        {
            if (node.SelectElements.Count == 1
             && node.FromClause == null)
            {
                TSqlFragment target = node.SelectElements[0];
                if (target is SelectSetVariable selectSetVariable)
                    target = selectSetVariable.Expression;

                if (this._scalarFunctionCalls.ContainsKey(target.StartOffset))
                    this._scalarFunctionCalls.Remove(target.StartOffset);
            }
        }

        private bool IsScalarFunctionCall(FunctionCall call) => base.Model.IsScalarFunction(call);

        private sealed class ScalarFunctionCallVisitor : TSqlFragmentVisitor
        {
            private readonly Func<FunctionCall, bool> _isScalarFunctionCallMethod;

            public ICollection<FunctionCall> Locations { get; }

            public ScalarFunctionCallVisitor(Func<FunctionCall, bool> isScalarFunctionCallMethod)
            {
                this._isScalarFunctionCallMethod = isScalarFunctionCallMethod;
                this.Locations = new Collection<FunctionCall>();
            }

            public override void Visit(FunctionCall node)
            {
                if (this._isScalarFunctionCallMethod(node))
                    this.Locations.Add(node);
            }
        }
    }
}