using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 38)]
    public sealed class RedundantSymbolSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private readonly IDictionary<string, DeclareVariableElement> _variables;
        private readonly IDictionary<int, ParameterReference> _parameterReferences;

        protected override string ErrorMessageTemplate => "Unused {0}: {1}";

        public RedundantSymbolSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context)
        {
            this._variables = new Dictionary<string, DeclareVariableElement>();
            this._parameterReferences = new Dictionary<int, ParameterReference>();
        }

        protected override void BeginBatch(TSqlBatch node)
        {
            VariableVisitor visitor = new VariableVisitor();
            node.Accept(visitor);
            this._variables.ReplaceWith(visitor.Variables.ToDictionary(x => x.VariableName.Value));
            this._parameterReferences.ReplaceWith(visitor.ParameterReferences.ToDictionary(x => x.Parameter.StartOffset));
        }

        public override void Visit(VariableReference node) => this._variables.Remove(node.Name);

        public override void Visit(StringLiteral node)
        {
            VisitXQuerySqlVariable(node);
        }

        protected override void EndBatch(TSqlBatch node)
        {
            foreach (DeclareVariableElement unusedVariable in this._variables.Values)
            {
                if (this._parameterReferences.TryGetValue(unusedVariable.StartOffset, out ParameterReference parameterReference))
                {
                    string suppressionKey = $"{parameterReference.BaseName}{parameterReference.Parameter.VariableName.Value}";
                    base.FailIfUnsuppressed(unusedVariable, suppressionKey, "parameter", unusedVariable.VariableName.Value);
                }
                else
                    base.Fail(unusedVariable, "variable", unusedVariable.VariableName.Value);
            }
        }

        // https://learn.microsoft.com/en-us/sql/xquery/xquery-extension-functions-sql-variable
        private void VisitXQuerySqlVariable(StringLiteral node)
        {
            MatchCollection matches = Regex.Matches(node.Value, """sql:variable\(["'](?<VariableName>[^"']+)["']""");
            foreach (Match match in matches)
            {
                if (!match.Success) 
                    continue;

                _variables.Remove(match.Groups["VariableName"].Value);
            }
        }

        private sealed class VariableVisitor : TSqlFragmentVisitor
        {
            public ICollection<DeclareVariableElement> Variables { get; }
            public ICollection<ParameterReference> ParameterReferences { get; }

            public VariableVisitor()
            {
                this.Variables = new Collection<DeclareVariableElement>();
                this.ParameterReferences = new Collection<ParameterReference>();
            }

            public override void Visit(DeclareVariableElement node) => this.Variables.Add(node);
            public override void Visit(FunctionStatementBody node) => this.AddParameterReferences(node.Name, node.Parameters);
            public override void Visit(ProcedureStatementBody node) => this.AddParameterReferences(node.ProcedureReference.Name, node.Parameters);

            private void AddParameterReferences(SchemaObjectName name, IEnumerable<ProcedureParameter> parameters)
            {
                foreach (ProcedureParameter parameter in parameters) 
                    this.ParameterReferences.Add(new ParameterReference(name.BaseIdentifier.Value, parameter));
            }
        }

        private sealed class ParameterReference
        {
            public string BaseName { get; }
            public ProcedureParameter Parameter { get; }

            public ParameterReference(string baseName, ProcedureParameter parameter)
            {
                this.BaseName = baseName;
                this.Parameter = parameter;
            }
        }
    }
}
