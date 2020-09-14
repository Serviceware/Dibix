using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 37)]
    public sealed class InlineFunctionSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private const bool AllowNonInlineTableValuedFunctions = false;
        private readonly IDictionary<int, FunctionCall> _scalarFunctionCalls = new Dictionary<int, FunctionCall>();

        // helpLine suppressions
        private static readonly IDictionary<string, string> Suppressions = new Dictionary<string, string>
        {
            ["fnSplit"] = "88d0aa3b3a42962c453a447f75ec497e"
          , ["hlsysattachment_query_data_case"] = "ee90fe914cfeb0db00052a05c8665c20"
          , ["hlsysattachment_query_data_contract"] = "443884f67d3aba91ff1e349cd5e9ecd0"
          , ["hlsysattachment_query_data_orgunit"] = "263a497418d134308f88fc9a614b7a41"
          , ["hlsysattachment_query_data_person"] = "5c96c38d8ace9da3946867a743344ecf"
          , ["hlsysattachment_query_data_product"] = "127f67d043cfa9c54a977813a2e5271b"
          , ["hlsysattachment_query_data_su"] = "a137b10bac21a9873f71e91e97c85420"
          , ["hlsysgetusercontext"] = "83638772ba6add448f0130e609531cf4"
          , ["hlsyssec_query_agentsystemacl"] = "40a02f1fc6a88990d13827c3567e5b15"
          , ["hlsysum_getcentraladminorgunits"] = "31d880c73db362bc15cf338c33dee422"
          , ["hltm_getreceiversfortask"] = "85d0a2eb6f719bb4c987c1ecaf4d327b"
        };

        protected override string ErrorMessageTemplate => "{0}";

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