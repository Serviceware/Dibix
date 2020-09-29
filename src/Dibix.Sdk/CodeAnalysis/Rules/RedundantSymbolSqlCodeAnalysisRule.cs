using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 38)]
    public sealed class RedundantSymbolSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private readonly IDictionary<string, DeclareVariableElement> _variables;
        private readonly ICollection<int> _suppressions;

        protected override string ErrorMessageTemplate => "Unused {0}: {1}";

        public RedundantSymbolSqlCodeAnalysisRule()
        {
            this._variables = new Dictionary<string, DeclareVariableElement>();
            this._suppressions = new HashSet<int>();
        }

        protected override void BeginBatch(TSqlBatch node)
        {
            VariableVisitor visitor = new VariableVisitor();
            node.Accept(visitor);
            this._variables.ReplaceWith(visitor.Variables.ToDictionary(x => x.VariableName.Value));
            this._suppressions.ReplaceWith(visitor.ParameterReferences.Where(x => base.IsSuppressed(x.Key)).Select(x => x.Value));
        }

        public override void Visit(VariableReference node) => this._variables.Remove(node.Name);

        protected override void EndBatch(TSqlBatch node)
        {
            foreach (DeclareVariableElement unusedVariable in this._variables.Values)
            {
                if (this._suppressions.Contains(unusedVariable.StartOffset))
                    continue;

                base.Fail(unusedVariable, unusedVariable is ProcedureParameter ? "parameter" : "variable", unusedVariable.VariableName.Value);
            }
        }

        private sealed class VariableVisitor : TSqlFragmentVisitor
        {
            public ICollection<DeclareVariableElement> Variables { get; }
            public IDictionary<string, int> ParameterReferences { get; }

            public VariableVisitor()
            {
                this.Variables = new Collection<DeclareVariableElement>();
                this.ParameterReferences = new Dictionary<string, int>();
            }

            public override void Visit(DeclareVariableElement node) => this.Variables.Add(node);
            public override void Visit(FunctionStatementBody node) => this.AddParameterReferences(node.Name, node.Parameters);
            public override void Visit(ProcedureStatementBody node) => this.AddParameterReferences(node.ProcedureReference.Name, node.Parameters);

            private void AddParameterReferences(SchemaObjectName name, IEnumerable<ProcedureParameter> parameters)
            {
                foreach (ProcedureParameter parameter in parameters) 
                    this.ParameterReferences.Add($"{name.BaseIdentifier.Value}{parameter.VariableName.Value}", parameter.StartOffset);
            }
        }
    }
}
