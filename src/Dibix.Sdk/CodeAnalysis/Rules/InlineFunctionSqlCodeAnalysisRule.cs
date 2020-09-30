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
            base.FailIfUnsuppressed(node, name, $"Make non inline table valued function inline or replace it with a stored procedure: {name}");
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