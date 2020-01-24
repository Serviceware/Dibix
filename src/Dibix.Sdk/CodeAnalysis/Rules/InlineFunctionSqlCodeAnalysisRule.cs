using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Dibix.Sdk.Sql;
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
            if (node.ReturnType is TableValuedFunctionReturnType && !AllowNonInlineTableValuedFunctions)
                base.Fail(node, $"Make non inline table valued function inline or replace it with a stored procedure: {node.Name.BaseIdentifier.Value}");
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
             && scalarSubquery.QueryExpression is QuerySpecification querySpecification
             && querySpecification.SelectElements.Count == 1)
            {
                SelectElement target = querySpecification.SelectElements[0];
                if (this._scalarFunctionCalls.ContainsKey(target.StartOffset))
                    this._scalarFunctionCalls.Remove(target.StartOffset);
            }
        }

        public override void Visit(SelectSetVariable node) => this.VisitScalarExpression(node.Expression);

        public override void Visit(SetVariableStatement node) => this.VisitScalarExpression(node.Expression);

        private void VisitScalarExpression(ScalarExpression node)
        {
            if (node == null)
                return;

            if (this._scalarFunctionCalls.ContainsKey(node.StartOffset))
                this._scalarFunctionCalls.Remove(node.StartOffset);
        }

        private bool IsScalarFunctionCall(FunctionCall call) => base.TryGetModelElement(call, out ElementLocation location) && base.Model.IsScalarFunction(location);

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